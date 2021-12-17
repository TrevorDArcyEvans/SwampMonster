namespace EventSwamp
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public sealed class ContactManager
  {
    private readonly HashSet<Contact> _contacts = new();

    public IEnumerable<Contact> Contacts => _contacts;

    public EventHandler OnContactAdded;
    public EventHandler OnContactRemoved;

    public void Add(Contact contact)
    {
      _contacts.Add(contact);
      contact.OnAddressChanged += OnAddressChanged;
      contact.OnFirstNameChanged += OnFirstNameChanged;
      contact.OnLastNameChanged += OnLastNameChanged;

      OnContactAdded?.Invoke(this, EventArgs.Empty);
    }

    public void Remove(int id)
    {
      var contact = _contacts.Single(c => c.Id == id);
      contact.OnAddressChanged -= OnAddressChanged;
      contact.OnFirstNameChanged -= OnFirstNameChanged;
      contact.OnLastNameChanged -= OnLastNameChanged;
      _contacts.Remove(contact);

      OnContactRemoved?.Invoke(this, EventArgs.Empty);
    }

    private void OnAddressChanged(object sender, Address address)
    {
      Console.WriteLine($"New address   : {address}");
    }

    private void OnFirstNameChanged(object sender, string e)
    {
      Console.WriteLine($"New first name: {e}");
    }

    private void OnLastNameChanged(object sender, string e)
    {
      Console.WriteLine($"New last  name: {e}");
    }
  }
}
