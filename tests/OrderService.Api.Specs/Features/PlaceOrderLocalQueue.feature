Feature: Place order with local queue strategy
  As a developer running the local/development messaging strategy
  I want place-order to enqueue into OrdersTopic in PostgreSQL
  So that I can verify queue-backed behavior without Azure Service Bus

  @ignore
  Scenario: Place order writes queue messages to OrdersTopic
    When I place an order with total items 2 using local queue strategy
    Then the place order response status code should be 200
    And the place order response should contain a valid order id
    And the local OrdersTopic should contain 3 messages for that order
