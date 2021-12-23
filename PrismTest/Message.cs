namespace PrismTest;

using System;

public sealed class Message
{
  public DateTime Sent { get; set; } = DateTime.UtcNow;
  public string Text { get; set; }
}
