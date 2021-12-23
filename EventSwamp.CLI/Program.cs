namespace EventSwamp.CLI
{
  using System;
  using System.Linq;
  using Faker;

  public static class Program
  {
    public static void Main(string[] args)
    {
      var mgr = new ContactManager();
      mgr.OnContactAdded += (s, e) => Console.WriteLine("OnContactAdded");
      mgr.OnContactRemoved += (s, e) => Console.WriteLine("OnContactRemoved");
      mgr.OnChanged += (s, e) => Console.WriteLine("OnChanged");

      // create
      for (var i = 0; i < 100; i++)
      {
        var address = new EventSwamp.Address(Address.StreetAddress(), Address.City(), Address.Country());
        var contact = new Contact(i, Name.First(), Name.Last(), address);

        mgr.Add(contact);
      }

      // update
      foreach (var contact in mgr.Contacts)
      {
        contact.FirstName = Name.First();
        contact.LastName = Name.Last();
        contact.Address.StreetAddress = Address.StreetAddress();
        contact.Address.City = Address.City();
        contact.Address.Country = Address.Country();
      }

      // delete
      foreach (var id in mgr.Contacts.Select(c => c.Id))
      {
        mgr.Remove(id);
      }
    }
  }
}
