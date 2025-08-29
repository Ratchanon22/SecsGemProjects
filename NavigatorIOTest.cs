using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== NavigatorIO Test ===");

        try
        {
            IIoController io = new NavigatorIO();

            // ตั้งค่า D0 เป็น HIGH
            io.SetOutput(0, true);
            Console.WriteLine("Set D0 to HIGH");

            // รอ 1 วินาที
            System.Threading.Thread.Sleep(1000);

            // อ่านค่ากลับจาก D0
            bool state = io.GetInput(0);
            Console.WriteLine("Read D0: " + (state ? "HIGH" : "LOW"));

            // ตั้งค่า D0 เป็น LOW
            io.SetOutput(0, false);
            Console.WriteLine("Set D0 to LOW");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test failed: " + ex.Message);
        }

        Console.WriteLine("=== Test Completed ===");
    }
}
