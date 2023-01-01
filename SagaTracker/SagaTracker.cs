namespace SagaTracker
{
    public class SagaTracker
    {
        private readonly Stack<Func<Task>> rollbackSteps;

        public SagaTracker()
        {
            rollbackSteps = new Stack<Func<Task>>();
        }

        /// <summary>
        /// Given a forward exectuion that returns a rollback execution, 
        /// execute forward and track the rollback.
        /// If forward execution fails, rollback entire saga.
        /// </summary>
        /// <param name="forwardStep"></param>
        public async Task ExecuteWithRollbackAsync(Func<Task<Func<Task>>> forwardStep)
        {
            try
            {
                var rollback = await forwardStep();
                rollbackSteps.Push(rollback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred. Rollback started.");
                await ExecuteRollback();
                Console.WriteLine($"Rollback Complete");
                throw;
            }
        }

        public async Task AddRollback(Func<Task> rollbackStep)
        {
            rollbackSteps.Push(rollbackStep);
        }

        public async Task ExecuteRollback()
        {
            int currentIndex = 0;
            int totalRollbackSteps = rollbackSteps.Count;

            while (rollbackSteps.Count > 0)
            {
                try
                {
                    var p = rollbackSteps.Pop();
                    await p();
                    currentIndex++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred while rollback at step {currentIndex} out of {totalRollbackSteps}.");
                    throw;
                }
            }
        }
    }
}
