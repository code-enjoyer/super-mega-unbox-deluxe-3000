using System;
using System.Collections.Generic;

namespace SuperMegaUnboxDeluxe.Domain;

public class User : Entity
{
    public List<UserItem> Items { get; set; } = new List<UserItem>();
}
