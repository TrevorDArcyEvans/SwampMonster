namespace EventSwamp;

using System;

public interface IHaveChanged
{
  event EventHandler OnChanged;
}
