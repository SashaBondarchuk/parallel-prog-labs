class BankAccount
{
    public int Id { get; set; }
    public decimal Balance { get; set; }
}

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int accountsCount = 150;
        int transactionsCount = 5000;
        Random rnd = new Random();

        var accounts = Enumerable.Range(1, accountsCount)
            .Select(i => new BankAccount { Id = i, Balance = rnd.Next(1000, 5000) })
            .ToList();

        decimal initialTotal = accounts.Sum(a => a.Balance);
        Console.WriteLine($"Початкова сума всіх коштів: {initialTotal}");

        //RunTransfers(accounts, transactionsCount, TransferWithRaceCondition);

        RunTransfers(accounts, transactionsCount, TransferWithDeadlock);

        //RunTransfers(accounts, transactionsCount, SafeTransfer);

        decimal finalTotal = accounts.Sum(a => a.Balance);
        Console.WriteLine($"Кінцева сума всіх коштів: {finalTotal}");
        Console.WriteLine($"Різниця: {finalTotal - initialTotal}");
    }

    static void RunTransfers(List<BankAccount> accounts, int count, Action<BankAccount, BankAccount, decimal> transferMethod)
    {
        Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 1000 }, i =>
        {
            Random localRnd = new Random(Guid.NewGuid().GetHashCode());
            var from = accounts[localRnd.Next(accounts.Count)];
            var to = accounts[localRnd.Next(accounts.Count)];
            decimal amount = localRnd.Next(10, 100);
            Console.WriteLine("Transfer: " + from.Id + " -> " + to.Id);
            if (from.Id != to.Id)
            {
                transferMethod(from, to, amount);
            }
            Console.WriteLine("Transfer: " + from.Id + " -> " + to.Id + ": " + " completed");
        });
    }

    // Race Condition
    static void TransferWithRaceCondition(BankAccount from, BankAccount to, decimal amount)
    {
        if (from.Balance >= amount)
        {
            from.Balance -= amount;
            to.Balance += amount;
        }
    }

    // Deadlock
    static void TransferWithDeadlock(BankAccount from, BankAccount to, decimal amount)
    {
        lock (from)
        {
            Thread.Sleep(1);
            lock (to)
            {
                if (from.Balance >= amount)
                {
                    from.Balance -= amount;
                    to.Balance += amount;
                }
            }
        }
    }

    // Ordered Locking 
    static void SafeTransfer(BankAccount from, BankAccount to, decimal amount)
    {
        var firstLock = from.Id < to.Id ? from : to;
        var secondLock = from.Id < to.Id ? to : from;

        lock (firstLock)
        {
            lock (secondLock)
            {
                if (from.Balance >= amount)
                {
                    from.Balance -= amount;
                    to.Balance += amount;
                }
            }
        }
    }
}