namespace EventSwamp
{
  using System;

  public sealed class Address
  {
    private string _streetAddress;
    private string _city;
    private string _country;

    public Address(
      string streetAddress,
      string city,
      string country)
    {
      _streetAddress = streetAddress;
      _city = city;
      _country = country;
    }

    public event EventHandler OnAddressChanged;

    public string StreetAddress
    {
      get => _streetAddress;

      set
      {
        if (_streetAddress == value)
        {
          return;
        }

        _streetAddress = value;
        OnAddressChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public string City
    {
      get => _city;

      set
      {
        if (_city == value)
        {
          return;
        }

        _city = value;
        OnAddressChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public string Country
    {
      get => _country;

      set
      {
        if (_country == value)
        {
          return;
        }

        _country = value;
        OnAddressChanged?.Invoke(this, EventArgs.Empty);
      }
    }

    public override string ToString()
    {
      return $"{StreetAddress}{Environment.NewLine}{City}{Environment.NewLine}{Country}";
    }
  }
}
