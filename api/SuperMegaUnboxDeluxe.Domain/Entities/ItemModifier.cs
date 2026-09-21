using System;
using System.Collections.Generic;
using System.Text;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class ItemModifier
{
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;

    private ItemModifier() { }

    public ItemModifier(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
