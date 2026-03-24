Feature: OrderService endpoints
  As a consumer of OrderService
  I want the API endpoints to respond with expected payloads
  So that I can trust basic service behavior

  Scenario: Ping endpoint returns default greeting
    When I send a GET request to "/api/ping"
    Then the response status code should be 200
    And the ping response service should be "OrderService"
    And the ping response message should contain "Hello World"

  Scenario: Ping endpoint returns personalized greeting
    When I send a GET request to "/api/ping?name=Chris"
    Then the response status code should be 200
    And the ping response message should contain "Hello Chris"

  Scenario: Get products returns seeded catalogue
    When I send a GET request to "/api/order/products"
    Then the response status code should be 200
    And the products response should contain at least 1 product
    And the products response should contain sku "SK1"

  Scenario: Get product by id returns an existing product
    When I send a GET request to "/api/order/products/1"
    Then the response status code should be 200
    And the product response sku should be "SK1"

  Scenario: Get product by id returns not found for missing id
    When I send a GET request to "/api/order/products/9999"
    Then the response status code should be 404
