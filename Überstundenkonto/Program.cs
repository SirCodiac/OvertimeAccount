using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Überstundenkonto.Properties;

namespace Überstundenkonto
{
  class Program
  {
    private static List<Entry> ENTRIES = new List<Entry>();
    private static readonly string PATH = $"{Environment.CurrentDirectory}\\Data";
    private static readonly string FILE = Path.Combine(PATH, "stor.age");
    static void Main(string[] args)
    {
      StartUp();
      Run();
    }
    private static void Run()
    {
      Console.WriteLine("Welcome to your overtime account.");
      if (Settings.Default.FirstStart)
      {
        Settings.Default.FirstStart = false;
        FirstConfigOvertimeLimit();
      }
      Menu();
    }
    private static void RemoveOvertime()
    {
      var end = false;
      while (!end)
      {
        Console.Clear();
        Console.WriteLine("List overtime list");
        Console.WriteLine("------------------");
        for (int i = 0; i < ENTRIES.Count; i++)
          Console.WriteLine($"{i + 1}) {ENTRIES[i]}");
        Console.WriteLine("------------------\n");
        Console.Write("Please enter the number to delete: ");
        int input;
        try
        {
          input = Convert.ToInt32(Console.ReadLine());
        } catch(Exception ex)
        {
          continue;
        }
        if (input < 1 || input > ENTRIES.Count + 1)
          continue;
        ENTRIES.RemoveAt(input - 1);
        SaveList();
        end = true;
      }
    }
    private static void AddOvertime()
    {
      var end = false;
      while (!end)
      {
        Console.Clear();
        try
        {
          Console.Write("Please enter a date by the following pattern (DD-MM-YYYY): ");
          var inputDate = Console.ReadLine();
          if (inputDate.Count(x => x.Equals('-')) != 2)
            continue;
          var splittedDate = inputDate.Split('-');
          if (!ValidateDate(Convert.ToInt32(splittedDate[0]), Convert.ToInt32(splittedDate[1]), Convert.ToInt32(splittedDate[2])))
            continue;
          var date = new DateTime(Convert.ToInt32(splittedDate[2]), Convert.ToInt32(splittedDate[1]), Convert.ToInt32(splittedDate[0]));

          Console.Write("Please enter the reason: ");
          var reason = Console.ReadLine();

          Console.Write("Please enter the overtime amount in hours: ");
          var input = Convert.ToDouble(Console.ReadLine());
          if (input < -64 || input > 64)
            continue;
          if (ENTRIES == null)
            ENTRIES = new List<Entry>();
          ENTRIES.Add(new Entry { Date = date, Reason = reason, Amount = input });
          SaveList();
          end = true;
          StartUp();
        }
        catch (Exception ex)
        {
          continue;
        }
      }
    }
    private static void Menu()
    {
      var end = false;
      while (!end)
      {
        Console.Clear();
        if (GetLimited() && GetOvertimeLimit() <= ENTRIES.Sum(x => x.Amount))
        {
          var oldColor = Console.ForegroundColor;
          Console.ForegroundColor = ConsoleColor.Red;
          Console.WriteLine("WARNING! OVERTIME LIMIT REACHED!");
          Console.ForegroundColor = oldColor;
        }
        Console.WriteLine("Please choose between the following options: ");
        Console.WriteLine("1.) Configure overtime limit");
        Console.WriteLine("2.) List overtime list");
        Console.WriteLine("3.) Add overtime entry");
        Console.WriteLine("4.) Remove overtime entry");
        Console.Write("Please enter the matching number from above: ");
        try
        {
          var input = Convert.ToInt32(Console.ReadLine());
          if (input < 1 || input > 4)
            Environment.Exit(0);
          switch (input)
          {
            case 1:
              ConfigureOvertimeLimit();
              break;
            case 2:
              ListOvertime();
              break;
            case 3:
              AddOvertime();
              break;
            case 4:
              RemoveOvertime();
              break;
          }
        }
        catch (Exception)
        {
          end = true;
        }
      }
    }
    private static void ListOvertime()
    {
      Console.Clear();
      Console.WriteLine("List overtime list");
      Console.WriteLine("------------------");
      foreach (var item in ENTRIES)
        Console.WriteLine(item);
      Console.Write("Press any key to continue...");
      Console.ReadKey();
    }
    private static void ConfigureOvertimeLimit()
    {
      var end = false;
      Console.Clear();
      if (GetLimited())
      {
        while (!end)
        {
          Console.WriteLine($"Current settings: Limiter activated. Limit at {GetOvertimeLimit()}h");
          Console.Write("Press '1' to disable limiter or press '2' to edit the limit or press '3' to go back.\nInput: ");
          try
          {
            var input = Convert.ToInt32(Console.ReadLine());
            if (input < 1 || input > 3)
              continue;
            if (input == 1)
            {
              SetLimited(false);
              return;
            }
            else if (input == 3)
              return;
            else
            {
              Console.Write("Please enter the new limit: ");
              var input2 = Convert.ToDouble(Console.ReadLine());
              if (input2 > -64 && input2 < 64)
                SetOvertimeLimit(input2);
              else
                continue;
            }
          }
          catch (Exception)
          {
            continue;
          }

        }
      }
      else
      {
        FirstConfigOvertimeLimit();
      }
    }

    private static void FirstConfigOvertimeLimit()
    {
      if (!GetLimited() || GetOvertimeLimit() == default(double))
      {
        var end = false;
        while (!end)
        {
          Console.Write("Do you want to set an overtime limit? (Y|N): ");
          var input = Console.ReadKey();
          if (input.Key.Equals(ConsoleKey.Y))
          {
            SetLimited(true);
            end = true;
          }
          else if (input.Key.Equals(ConsoleKey.N))
          {
            SetLimited(false);
            end = true;
          }
        }
        end = false;
        while (!end && GetLimited())
        {
          Console.Write("\nPlease enter the overtime limit: ");
          try
          {
            var input = Convert.ToDouble(Console.ReadLine());
            if (input < 0 || input > 64)
              continue;
            SetOvertimeLimit(input);
            end = true;
          }
          catch (Exception)
          {
            continue;
          }
        }
      }
    }
    private static void StartUp()
    {
      if (!Directory.Exists(PATH))
        Directory.CreateDirectory(PATH);
      if (!File.Exists(FILE))
        File.Create(FILE).Close();
      else
        ENTRIES = JsonConvert.DeserializeObject<List<Entry>>(File.ReadAllText(FILE));

      Console.Title = $"Overtime account";
      if (ENTRIES?.Sum(x => x.Amount) > 0)
        Console.Title += $" - {ENTRIES.Sum(x => x.Amount)}h";
    }
    private static void SaveList() => File.WriteAllText(FILE, JsonConvert.SerializeObject(ENTRIES, Formatting.Indented));
    private static void SetOvertimeLimit(double amount)
    {
      Properties.Settings.Default.MaxOvertime = amount;
      Properties.Settings.Default.Save();
    }
    private static double GetOvertimeLimit() => Properties.Settings.Default.MaxOvertime;
    private static void SetLimited(bool limited) { Properties.Settings.Default.LimitOvertime = limited; Properties.Settings.Default.Save(); }
    private static bool GetLimited() => Properties.Settings.Default.LimitOvertime;
    private static bool ValidateDate(int day, int month, int year)
    {
      if (day == default || month == default || year == default || year < 2000)
        return false;
      if (month < 1 || month > 12)
        return false;
      if (month != 2)
      {
        if ((month == 1 || month == 3 || month == 5 || month == 7 || month == 8 || month == 10 || month == 12) && day < 1 || day > 31)
          return false;
        else if (day < 1 || day > 30)
          return false;
        else return true;
      }
      else
      {
        if (DateTime.IsLeapYear(year) && day < 1 || day > 29) return false;
        else if (day < 1 || day > 28) return false;
        else return true;
      }
    }
  }
  internal class Entry
  {
    public DateTime Date { get; set; }
    public string Reason { get; set; }
    public double Amount { get; set; }
    public override string ToString() => $"{Date.ToShortDateString()} - {Reason}: {Amount}h";
  }
}
