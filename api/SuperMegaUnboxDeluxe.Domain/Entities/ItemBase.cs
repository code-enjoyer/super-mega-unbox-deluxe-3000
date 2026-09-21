using System;
using System.Collections.Generic;
using System.Text;

namespace SuperMegaUnboxDeluxe.Domain.Entities;

public class ItemBase : Entity
{
    public string Name { get; private set; } = null!;

    private ItemBase() { }

    public ItemBase(string name)
    {
        Name = name;
    }
}
