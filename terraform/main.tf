terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.20"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "azurerm" {
  features {}
}

locals {
  project_name = "servicethebus"
  location     = "uksouth"
}

variable "container_app_image_placeholder" {
  type        = string
  description = "Bootstrap image used only to satisfy Container App creation. Replace later via deployment script."
  default     = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
}

resource "random_string" "acr_suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "random_string" "storage_suffix" {
  length  = 8
  special = false
  upper   = false
}

resource "random_string" "postgres_suffix" {
  length  = 8
  special = false
  upper   = false
}

resource "random_password" "postgres_admin" {
  length  = 24
  special = false
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${local.project_name}"
  location = local.location
}

resource "azurerm_log_analytics_workspace" "law" {
  name                = "law-${local.project_name}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_storage_account" "functions" {
  name                          = "st${local.project_name}${random_string.storage_suffix.result}"
  resource_group_name           = azurerm_resource_group.rg.name
  location                      = azurerm_resource_group.rg.location
  account_tier                  = "Standard"
  account_replication_type      = "LRS"
  min_tls_version               = "TLS1_2"
  allow_nested_items_to_be_public = false
}

resource "azurerm_postgresql_flexible_server" "orderservice" {
  name                   = "pg-${local.project_name}-${random_string.postgres_suffix.result}"
  resource_group_name    = azurerm_resource_group.rg.name
  location               = azurerm_resource_group.rg.location
  version                = "16"
  administrator_login    = "pgadminuser"
  administrator_password = random_password.postgres_admin.result
  sku_name               = "B_Standard_B1ms"
  storage_mb             = 32768
  zone                   = "1"
}

resource "azurerm_postgresql_flexible_server_database" "orderservice" {
  name      = "orderservice"
  server_id = azurerm_postgresql_flexible_server.orderservice.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_azure_services" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.orderservice.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

locals {
  orderservice_postgres_connection_string = "Host=${azurerm_postgresql_flexible_server.orderservice.fqdn};Port=5432;Database=${azurerm_postgresql_flexible_server_database.orderservice.name};Username=${azurerm_postgresql_flexible_server.orderservice.administrator_login};Password=${random_password.postgres_admin.result};SSL Mode=Require;Trust Server Certificate=true"
}

resource "azurerm_container_app_environment" "cae" {
  name                       = "cae-${local.project_name}"
  location                   = azurerm_resource_group.rg.location
  resource_group_name        = azurerm_resource_group.rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.law.id
}

resource "azurerm_container_registry" "acr" {
  name                = "${local.project_name}acr${random_string.acr_suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Basic"
  admin_enabled       = false
}

resource "azurerm_servicebus_namespace" "sb" {
  name                = "sb-${local.project_name}"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"
}

resource "azurerm_servicebus_topic" "orders" {
  name         = "orders-topic"
  namespace_id = azurerm_servicebus_namespace.sb.id
}

resource "azurerm_servicebus_subscription" "orders_eng" {
  name               = "orders-eng"
  topic_id           = azurerm_servicebus_topic.orders.id
  max_delivery_count = 10
}

resource "azurerm_servicebus_subscription" "orders_sco" {
  name               = "orders-sco"
  topic_id           = azurerm_servicebus_topic.orders.id
  max_delivery_count = 10
}

resource "azurerm_servicebus_subscription" "orders_wal" {
  name               = "orders-wal"
  topic_id           = azurerm_servicebus_topic.orders.id
  max_delivery_count = 10
}

resource "azurerm_servicebus_queue" "supplier_quotes" {
  name         = "supplier-quotes"
  namespace_id = azurerm_servicebus_namespace.sb.id
}

resource "azurerm_servicebus_namespace_authorization_rule" "apps" {
  name         = "apps-access"
  namespace_id = azurerm_servicebus_namespace.sb.id

  listen = true
  send   = true
  manage = true
}

data "azurerm_role_definition" "acr_pull" {
  name  = "AcrPull"
  scope = azurerm_container_registry.acr.id
}

# -----------------------------------------------------------------------------
# orderservice-api
# -----------------------------------------------------------------------------
resource "azurerm_user_assigned_identity" "orderservice_api_pull" {
  name                = "orderservice-api-pull-id"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "random_uuid" "orderservice_api_acr_pull" {}

resource "azurerm_role_assignment" "orderservice_api_acr_pull" {
  scope              = azurerm_container_registry.acr.id
  role_definition_id = data.azurerm_role_definition.acr_pull.id
  principal_id       = azurerm_user_assigned_identity.orderservice_api_pull.principal_id
  name               = random_uuid.orderservice_api_acr_pull.result
}

resource "azurerm_container_app" "orderservice_api" {
  name                         = "orderservice-api"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.cae.id
  revision_mode                = "Single"

  depends_on = [azurerm_role_assignment.orderservice_api_acr_pull]

  secret {
    name  = "servicebus-connection"
    value = azurerm_servicebus_namespace_authorization_rule.apps.primary_connection_string
  }

  secret {
    name  = "orderservice-postgres-connection"
    value = local.orderservice_postgres_connection_string
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.orderservice_api_pull.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.orderservice_api_pull.id
  }

  ingress {
  external_enabled = true
  target_port      = 80

  traffic_weight {
    latest_revision = true
    percentage      = 100
  }
}


  template {
    container {
      name   = "orderservice-api"
      image  = var.container_app_image_placeholder
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "AzureServiceBus__ConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name  = "AzureServiceBus__TopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "AzureServiceBus__SupplierQuotesQueueName"
        value = azurerm_servicebus_queue.supplier_quotes.name
      }

      env {
        name        = "ConnectionStrings__OrderServiceDb"
        secret_name = "orderservice-postgres-connection"
      }

      env {
        name  = "MessageProcessing__Mode"
        value = "Function"
      }
    }
  }
}

# -----------------------------------------------------------------------------
# orderservice-func
# orderservice-func
# -----------------------------------------------------------------------------
resource "azurerm_user_assigned_identity" "orderservice_function_pull" {
  name                = "orderservice-func-pull-id"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "random_uuid" "orderservice_function_acr_pull" {}

resource "azurerm_role_assignment" "orderservice_function_acr_pull" {
  scope              = azurerm_container_registry.acr.id
  role_definition_id = data.azurerm_role_definition.acr_pull.id
  principal_id       = azurerm_user_assigned_identity.orderservice_function_pull.principal_id
  name               = random_uuid.orderservice_function_acr_pull.result
}

resource "azurerm_container_app" "orderservice_function" {
  name                         = "orderservice-func"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.cae.id
  revision_mode                = "Single"

  depends_on = [azurerm_role_assignment.orderservice_function_acr_pull]

  secret {
    name  = "servicebus-connection"
    value = azurerm_servicebus_namespace_authorization_rule.apps.primary_connection_string
  }

  secret {
    name  = "azurewebjobs-storage"
    value = azurerm_storage_account.functions.primary_connection_string
  }

  secret {
    name  = "orderservice-postgres-connection"
    value = local.orderservice_postgres_connection_string
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.orderservice_function_pull.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.orderservice_function_pull.id
  }

  template {
    container {
      name   = "orderservice-func"
      image  = var.container_app_image_placeholder
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "AzureServiceBus__ConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name        = "AzureServiceBusConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name  = "AzureServiceBus__TopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "AzureServiceBus__SupplierQuotesQueueName"
        value = azurerm_servicebus_queue.supplier_quotes.name
      }

      env {
        name  = "AzureServiceBusSupplierQuotesQueueName"
        value = azurerm_servicebus_queue.supplier_quotes.name
      }

      env {
        name        = "ConnectionStrings__OrderServiceDb"
        secret_name = "orderservice-postgres-connection"
      }

      env {
        name  = "MessageProcessing__Mode"
        value = "Function"
      }

      env {
        name  = "FUNCTIONS_WORKER_RUNTIME"
        value = "dotnet-isolated"
      }

      env {
        name        = "AzureWebJobsStorage"
        secret_name = "azurewebjobs-storage"
      }
    }
  }
}

# -----------------------------------------------------------------------------
# suppliereng-func
# -----------------------------------------------------------------------------
resource "azurerm_user_assigned_identity" "suppliereng_function_pull" {
  name                = "suppliereng-func-pull-id"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "random_uuid" "suppliereng_function_acr_pull" {}

resource "azurerm_role_assignment" "suppliereng_function_acr_pull" {
  scope              = azurerm_container_registry.acr.id
  role_definition_id = data.azurerm_role_definition.acr_pull.id
  principal_id       = azurerm_user_assigned_identity.suppliereng_function_pull.principal_id
  name               = random_uuid.suppliereng_function_acr_pull.result
}

resource "azurerm_container_app" "suppliereng_function" {
  name                         = "suppliereng-func"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.cae.id
  revision_mode                = "Single"

  depends_on = [azurerm_role_assignment.suppliereng_function_acr_pull]

  secret {
    name  = "servicebus-connection"
    value = azurerm_servicebus_namespace_authorization_rule.apps.primary_connection_string
  }

  secret {
    name  = "azurewebjobs-storage"
    value = azurerm_storage_account.functions.primary_connection_string
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.suppliereng_function_pull.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.suppliereng_function_pull.id
  }

  template {
    container {
      name   = "suppliereng-func"
      image  = var.container_app_image_placeholder
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "SupplierServiceBus__ConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name        = "SupplierServiceBusConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name  = "SupplierServiceBus__OrdersTopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "SupplierServiceBusOrdersTopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "SupplierServiceBus__OrdersSubscriptionName"
        value = "orders-eng"
      }

      env {
        name  = "SupplierServiceBusOrdersSubscriptionName"
        value = "orders-eng"
      }

      env {
        name  = "SupplierServiceBus__SupplierQuotesQueueName"
        value = azurerm_servicebus_queue.supplier_quotes.name
      }

      env {
        name  = "SupplierServiceBus__SupplierCode"
        value = "ENG"
      }

      env {
        name  = "SupplierServiceBus__ProductsCsvPath"
        value = "products.csv"
      }

      env {
        name  = "MessageProcessing__Mode"
        value = "Function"
      }

      env {
        name  = "FUNCTIONS_WORKER_RUNTIME"
        value = "dotnet-isolated"
      }

      env {
        name        = "AzureWebJobsStorage"
        secret_name = "azurewebjobs-storage"
      }
    }

    min_replicas = 0
    max_replicas = 10

    custom_scale_rule {
      name             = "servicebus-scale-rule"
      custom_rule_type = "azure-servicebus"
      metadata = {
        topicName        = azurerm_servicebus_topic.orders.name
        subscriptionName = azurerm_servicebus_subscription.orders_eng.name
        messageCount     = "1"
      }
      authentication {
        secret_name       = "servicebus-connection"
        trigger_parameter = "connection"
      }
    }
  }
}

# -----------------------------------------------------------------------------
# suppliersco-func
# -----------------------------------------------------------------------------
resource "azurerm_user_assigned_identity" "suppliersco_function_pull" {
  name                = "suppliersco-func-pull-id"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "random_uuid" "suppliersco_function_acr_pull" {}

resource "azurerm_role_assignment" "suppliersco_function_acr_pull" {
  scope              = azurerm_container_registry.acr.id
  role_definition_id = data.azurerm_role_definition.acr_pull.id
  principal_id       = azurerm_user_assigned_identity.suppliersco_function_pull.principal_id
  name               = random_uuid.suppliersco_function_acr_pull.result
}

resource "azurerm_container_app" "suppliersco_function" {
  name                         = "suppliersco-func"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.cae.id
  revision_mode                = "Single"

  depends_on = [azurerm_role_assignment.suppliersco_function_acr_pull]

  secret {
    name  = "servicebus-connection"
    value = azurerm_servicebus_namespace_authorization_rule.apps.primary_connection_string
  }

  secret {
    name  = "azurewebjobs-storage"
    value = azurerm_storage_account.functions.primary_connection_string
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.suppliersco_function_pull.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.suppliersco_function_pull.id
  }

  template {
    container {
      name   = "suppliersco-func"
      image  = var.container_app_image_placeholder
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "SupplierServiceBus__ConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name        = "SupplierServiceBusConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name  = "SupplierServiceBus__OrdersTopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "SupplierServiceBusOrdersTopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "SupplierServiceBus__OrdersSubscriptionName"
        value = "orders-sco"
      }

      env {
        name  = "SupplierServiceBusOrdersSubscriptionName"
        value = "orders-sco"
      }

      env {
        name  = "SupplierServiceBus__SupplierQuotesQueueName"
        value = azurerm_servicebus_queue.supplier_quotes.name
      }

      env {
        name  = "SupplierServiceBus__SupplierCode"
        value = "SCO"
      }

      env {
        name  = "SupplierServiceBus__ProductsCsvPath"
        value = "products.csv"
      }

      env {
        name  = "MessageProcessing__Mode"
        value = "Function"
      }

      env {
        name  = "FUNCTIONS_WORKER_RUNTIME"
        value = "dotnet-isolated"
      }

      env {
        name        = "AzureWebJobsStorage"
        secret_name = "azurewebjobs-storage"
      }
    }

    min_replicas = 0
    max_replicas = 10

    custom_scale_rule {
      name             = "servicebus-scale-rule"
      custom_rule_type = "azure-servicebus"
      metadata = {
        topicName        = azurerm_servicebus_topic.orders.name
        subscriptionName = azurerm_servicebus_subscription.orders_sco.name
        messageCount     = "1"
      }
      authentication {
        secret_name       = "servicebus-connection"
        trigger_parameter = "connection"
      }
    }
  }
}

# -----------------------------------------------------------------------------
# supplierwal-func
# -----------------------------------------------------------------------------
resource "azurerm_user_assigned_identity" "supplierwal_function_pull" {
  name                = "supplierwal-func-pull-id"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "random_uuid" "supplierwal_function_acr_pull" {}

resource "azurerm_role_assignment" "supplierwal_function_acr_pull" {
  scope              = azurerm_container_registry.acr.id
  role_definition_id = data.azurerm_role_definition.acr_pull.id
  principal_id       = azurerm_user_assigned_identity.supplierwal_function_pull.principal_id
  name               = random_uuid.supplierwal_function_acr_pull.result
}

resource "azurerm_container_app" "supplierwal_function" {
  name                         = "supplierwal-func"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.cae.id
  revision_mode                = "Single"

  depends_on = [azurerm_role_assignment.supplierwal_function_acr_pull]

  secret {
    name  = "servicebus-connection"
    value = azurerm_servicebus_namespace_authorization_rule.apps.primary_connection_string
  }

  secret {
    name  = "azurewebjobs-storage"
    value = azurerm_storage_account.functions.primary_connection_string
  }

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.supplierwal_function_pull.id]
  }

  registry {
    server   = azurerm_container_registry.acr.login_server
    identity = azurerm_user_assigned_identity.supplierwal_function_pull.id
  }

  template {
    container {
      name   = "supplierwal-func"
      image  = var.container_app_image_placeholder
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name        = "SupplierServiceBus__ConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name        = "SupplierServiceBusConnectionString"
        secret_name = "servicebus-connection"
      }

      env {
        name  = "SupplierServiceBus__OrdersTopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "SupplierServiceBusOrdersTopicName"
        value = azurerm_servicebus_topic.orders.name
      }

      env {
        name  = "SupplierServiceBus__OrdersSubscriptionName"
        value = "orders-wal"
      }

      env {
        name  = "SupplierServiceBusOrdersSubscriptionName"
        value = "orders-wal"
      }

      env {
        name  = "SupplierServiceBus__SupplierQuotesQueueName"
        value = azurerm_servicebus_queue.supplier_quotes.name
      }

      env {
        name  = "SupplierServiceBus__SupplierCode"
        value = "WAL"
      }

      env {
        name  = "SupplierServiceBus__ProductsCsvPath"
        value = "products.csv"
      }

      env {
        name  = "MessageProcessing__Mode"
        value = "Function"
      }

      env {
        name  = "FUNCTIONS_WORKER_RUNTIME"
        value = "dotnet-isolated"
      }

      env {
        name        = "AzureWebJobsStorage"
        secret_name = "azurewebjobs-storage"
      }
    }

    min_replicas = 0
    max_replicas = 10

    custom_scale_rule {
      name             = "servicebus-scale-rule"
      custom_rule_type = "azure-servicebus"
      metadata = {
        topicName        = azurerm_servicebus_topic.orders.name
        subscriptionName = azurerm_servicebus_subscription.orders_wal.name
        messageCount     = "1"
      }
      authentication {
        secret_name       = "servicebus-connection"
        trigger_parameter = "connection"
      }
    }
  }
}

output "orderservice_api_url" {
  value = azurerm_container_app.orderservice_api.latest_revision_fqdn
}

output "container_registry_login_server" {
  value = azurerm_container_registry.acr.login_server
}

output "orderservice_postgres_server_fqdn" {
  value = azurerm_postgresql_flexible_server.orderservice.fqdn
}

output "orderservice_postgres_database_name" {
  value = azurerm_postgresql_flexible_server_database.orderservice.name
}

output "orderservice_postgres_admin_username" {
  value = azurerm_postgresql_flexible_server.orderservice.administrator_login
}

output "orderservice_postgres_connection_string" {
  value     = local.orderservice_postgres_connection_string
  sensitive = true
}
