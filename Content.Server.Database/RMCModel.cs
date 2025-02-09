using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("rmc_named_items")]
public sealed class RMCNamedItems
{
    [Key, ForeignKey("Profile")]
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = default!;

    [StringLength(20)]
    public string? PrimaryGunName { get; set; } = default!;

    [StringLength(20)]
    public string? SidearmName { get; set; } = default!;

    [StringLength(20)]
    public string? HelmetName { get; set; } = default!;

    [StringLength(20)]
    public string? ArmorName { get; set; } = default!;

    [StringLength(20)]
    public string? SentryName { get; set; } = default!;
}

[Table("rmc_squad_preferences")]
public sealed class RMCSquadPreference
{
    [Key, ForeignKey("Player")]
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = default!;

    public string? Squad { get; set; } // EntProtoId<SquadTeamComponent>
}

[Table("rmc_commendations")]
public sealed class RMCCommendation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id;

    [ForeignKey("Giver")]
    public Guid GiverId { get; set; }

    public Player Giver { get; set; } = default!;

    [ForeignKey("Receiver")]
    public Guid ReceiverId { get; set; }

    public Player Receiver { get; set; } = default!;

    [ForeignKey("Round")]
    public int RoundId { get; set; }

    public Round Round { get; set; } = default!;

    public string GiverName { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public CommendationType Type { get; set; }
}
