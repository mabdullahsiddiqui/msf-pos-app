using System;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] bytes = new byte[48];
            rng.GetBytes(bytes);
            string secret = Convert.ToBase64String(bytes);
            Console.WriteLine(secret);
        }
    }
}

