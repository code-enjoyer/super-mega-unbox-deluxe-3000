using System;
using System.Collections.Generic;
using System.Text;
using SuperMegaUnboxDeluxe.Domain.Items.Generation;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class ItemModifier
{
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public ModifierDetails? Details { get; private set; }

    private ItemModifier() { }

    public ItemModifier(string name, string value, ModifierDetails? details = null)
    {
        Name = name;
        Value = value;
        Details = details;
    }
}
