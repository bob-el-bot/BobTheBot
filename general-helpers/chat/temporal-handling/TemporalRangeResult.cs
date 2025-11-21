using System;

namespace BobTheBot.Chat.TemporalHandling;

public readonly record struct TemporalRangeResult(
    TemporalMode Mode,
    DateTime? From,
    DateTime? To
);