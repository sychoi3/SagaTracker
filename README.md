# SagaTracker
Data structure to track a series of forward executions with its corresponding rollback executions as a saga. 

## Purpose
The goal of this data structure is to provide a mechanism to allow forward execution of a series of steps wrapped in a pseudo transaction that can be rolled back if any forward execution fails. 

## Example
Let's say there is a company that awards licenses to its users for a product. A license is awarded by performing the following actions in a single transaction (all-or-nothing):

1. (UserCatalog API) Add product to users catalog.
    * Forward: Add product to user catalog.
    * Rollback: Remove product from user catalog.
2. (DB) Decrement the number of available licenses.
    * Forward: Decrement the available license count by -1.
    * Rollback: Increment the available license count by +1.
3. (License API) Assign license to user
    * Forward: Assign license to user.
    * Rollback: no action.

For example: If the series fails at **step 3**, then the available license count should be incremented back by +1 and the product should be removed from the user catalog.

## Limitations
This data structure is primarily useful in smaller use cases that require small bits of execution to be wrapped in a transaction and loses its usefulness at the higher feature level which may require asynchronous processing, retry policies, failure notifications, etc.

Some obvious limitations:
1. The fatter the transactions, the more brittle.
2. Higher dependency on resilient rollback execution.

## Usage
```cs
SagaTracker saga = new SagaTracker();

// Add product to user's catalog.
await saga.ExecuteWithRollbackAsync(async () =>
{
    await userCatalogClient.AddProductToUserCatalogAsync("user1", "product1");

    return async () => await userCatalogClient.RemoveProductFromUserCatalogAsync("user1", "product1");
});

// Decrement license pool count.
await saga.ExecuteWithRollbackAsync(async () =>
{
    var licensePool = await DbClient.GetLicensePoolAsync("license1");
    licensePool.Count--;
    await DbClient.UpdateLicensePoolAsync(licensePool);

    return async () =>{
        licensePool.Count++;
        await DbClient.UpdateLicensePoolAsync(licensePool);
    }
});

// Assign license to user.
await saga.ExecuteWithRollbackAsync(async () =>
{
    await licenseClient.AssignLicenseAsync("user1", "product1");

    return null;
});
```