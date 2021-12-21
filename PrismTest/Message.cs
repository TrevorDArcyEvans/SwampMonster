using System;

namespace PrismTest
{
  public sealed class Message
  {
    public DateTime Sent { get; set; } = DateTime.UtcNow;
    public string Text { get; set; }
  }
}
