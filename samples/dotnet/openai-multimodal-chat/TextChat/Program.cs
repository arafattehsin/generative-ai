namespace TextChat
{
    class Program
    {
        static void Main(string[] args)
        {
            AOIHelper serviceHelper = new AOIHelper();

            Console.WriteLine("Welcome to the Flight Bot. I can track your flights in seconds!");
            while (true) // You can have your own criteria to stop. 
            {
                var response = serviceHelper.GetResponse(Console.ReadLine());

                if(response == "stop")
                {
                    break;
                }

                Console.WriteLine(response);
            }
        }
    }
}