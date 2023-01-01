using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SagaTrackerTest
{
    public class SagaTrackerTests
    {
        [Fact]
        public async Task SagaTrackerCanExecute()
        {

            var events = new List<string>();

            var saga = new SagaTracker.SagaTracker();

            await saga.ExecuteWithRollbackAsync(async () =>
            {
                events.Add($"execution 1");
                return async () => events.Add("rollback 1");
            });

            await saga.ExecuteWithRollbackAsync(async () =>
            {
                events.Add($"execution 2");
                return async () => events.Add("rollback 2");
            });

            Assert.Equal(2, events.Count);
            Assert.Equal("execution 1", events[0]);
            Assert.Equal("execution 2", events[1]);
        }

        [Fact]
        public async Task SagaTrackerCanRollback()
        {

            var events = new List<string>();

            var saga = new SagaTracker.SagaTracker();

            await saga.ExecuteWithRollbackAsync(async () =>
            {
                events.Add($"execution 1");
                return async () => events.Add("rollback 1");
            });

            await saga.ExecuteWithRollbackAsync(async () =>
            {
                events.Add($"execution 2");
                return async () => events.Add("rollback 2");
            });

            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await saga.ExecuteWithRollbackAsync(async () =>
                {
                    throw new Exception("some execution error");
                    return async () => events.Add("rollback 3");
                }));

            Assert.Equal("some execution error", ex.Message);
            Assert.Equal(4, events.Count);
            Assert.Equal("execution 1", events[0]);
            Assert.Equal("execution 2", events[1]);
            Assert.Equal("rollback 2", events[2]);
            Assert.Equal("rollback 1", events[3]);
        }

        [Fact]
        public async Task SagaTrackerHandlesErrorDuringRollback()
        {

            var events = new List<string>();

            var saga = new SagaTracker.SagaTracker();

            await saga.ExecuteWithRollbackAsync(async () =>
            {
                events.Add($"execution 1");
                return async () => events.Add("rollback 1");
            });

            await saga.ExecuteWithRollbackAsync(async () =>
            {
                events.Add($"execution 2");
                return async () => throw new Exception("some rollback error");
            });

            var ex = await Assert.ThrowsAsync<Exception>(async () =>
                await saga.ExecuteWithRollbackAsync(async () =>
                {
                    throw new Exception("some execution error");
                    return async () => events.Add("rollback 3");
                }));

            Assert.Equal("some rollback error", ex.Message);
            Assert.Equal(2, events.Count);
            Assert.Equal("execution 1", events[0]);
            Assert.Equal("execution 2", events[1]);
        }
    }
}