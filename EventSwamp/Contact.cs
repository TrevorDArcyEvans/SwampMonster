namespace EventSwamp
{
  using System;

  public sealed class Contact
  {
    private readonly int _id;
    private string _firstName;
    private string _lastName;
    private Address _address;

    public event EventHandler<string> OnFirstNameChanged;
    public event EventHandler<string> OnLastNameChanged;
    public event EventHandler<Address> OnAddressChanged;

    public Contact(
      int id,
      string firstName,
      string lastName,
      Address address)
    {
      _id = id;
      _firstName = firstName;
      _lastName = lastName;
      _address = address;

      _address.OnAddressChanged += (s, e) => OnAddressChanged?.Invoke(this, Address);
    }

    public int Id => _id;

    public string FirstName
    {
      get => _firstName;

      set
      {
        if (_firstName == value)
        {
          return;
        }

        _firstName = value;
        OnFirstNameChanged?.Invoke(this, FirstName);
      }
    }

    public string LastName
    {
      get => _lastName;

      set
      {
        if (_lastName == value)
        {
          return;
        }

        _lastName = value;
        OnLastNameChanged?.Invoke(this, LastName);
      }
    }

    public Address Address => _address;

    #region Equals + GetHashCode

    protected bool Equals(Contact other)
    {
      return _id == other._id;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != this.GetType()) return false;
      return Equals((Contact)obj);
    }

    public override int GetHashCode()
    {
      return _id;
    }

    public static bool operator ==(Contact left, Contact right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(Contact left, Contact right)
    {
      return !Equals(left, right);
    }

    #endregion
  }
}
