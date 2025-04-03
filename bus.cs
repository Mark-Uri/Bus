
namespace ConsoleApp36
{


    class PassengerStop
    {
        private int waitingPassengers = 0;
        private static object lockObject = new object();
        private SemaphoreSlim semaphore;




        public PassengerStop(int maxBusCapacity)
        {
            semaphore = new SemaphoreSlim(maxBusCapacity);
        }



        public void AddPassengers(int count)
        {

            lock (lockObject)
            {
                waitingPassengers += count;
                Console.WriteLine($"На остановке прибавилось {count} пассажиров Теперь их {waitingPassengers}");
            }
        }

        public int LoadPassengers(int capacity)
        {

            int boarded = 0;



            lock (lockObject)
            {
                boarded = Math.Min(capacity, waitingPassengers);
                waitingPassengers -= boarded;
            }

            return boarded;
        }

        public int GetWaitingCount()
        {
            lock (lockObject)
                return waitingPassengers;
        }

        public SemaphoreSlim GetSemaphore() => semaphore;
    }







    class Bus
    {
        private int capacity;
        private string number;


        private PassengerStop stop;
        private Barrier barrier;
        private ManualResetEvent passengersReadyEvent;





        public Bus(string number, int capacity, PassengerStop stop, ManualResetEvent eventSignal)
        {
            this.number = number;
            this.capacity = capacity;
            this.stop = stop;
            this.passengersReadyEvent = eventSignal;

           
            barrier = new Barrier(1);
        }

        public void Start()
        {
            new Thread(Run).Start();
        }


        private void Run()
        {
            while (true)
            {
                passengersReadyEvent.WaitOne();
                passengersReadyEvent.Reset();

                Console.WriteLine($"\nАвтобус №{number} подъехал");

                int waiting = stop.GetWaitingCount();
                int toLoad = Math.Min(capacity, waiting);

                if (toLoad == 0)
                {
                    Console.WriteLine("Никого нет Автобус уехал пустой");
                    Thread.Sleep(2000);
                    continue;
                }

                int boarded = stop.LoadPassengers(toLoad); 

                for (int i = 0; i < boarded; i++)
                {
                    new Thread(() =>
                    {
                        stop.GetSemaphore().Wait(); 
                        Console.WriteLine($"Пассажир сел в автобус №{number}");
                        Thread.Sleep(100);
                        stop.GetSemaphore().Release();
                    }).Start();
                }

                Console.WriteLine($"Автобус №{number} забрал {boarded} пассажиров Осталось {stop.GetWaitingCount()}");
                Console.WriteLine($"Автобус №{number} уехал в рейс...");
                Thread.Sleep(4000);
                Console.WriteLine($"Автобус №{number} вернулся с рейса\n");
            }
        }


    }

    class Dispatcher
    {
        private PassengerStop stop;
        private ManualResetEvent eventSignal;
        private Random rand = new();

        public Dispatcher(PassengerStop stop, ManualResetEvent eventSignal)
        {
            this.stop = stop;
            this.eventSignal = eventSignal;
        }



        public void Start()
        {
            new Thread(Run).Start();
        }

        private void Run()
        {
            while (true)
            {
                Thread.Sleep(rand.Next(2000, 5000));
                int newPassengers = rand.Next(1, 10);
                stop.AddPassengers(newPassengers);
                eventSignal.Set(); 
            }
        }
    }












    internal class Program
    {
        static void Main(string[] args)
        {
            var stop = new PassengerStop(maxBusCapacity: 10);
            var eventSignal = new ManualResetEvent(false);
            var dispatcher = new Dispatcher(stop, eventSignal);
            var bus = new Bus("150", 10, stop, eventSignal);

            dispatcher.Start();
            bus.Start();

            Console.ReadLine();
        }
    }
}
