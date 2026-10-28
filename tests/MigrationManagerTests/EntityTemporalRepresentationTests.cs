using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Editor.Migration.Tests;

public sealed class EntityTemporalRepresentationTests
{
    private sealed class Probe:Entity
    {
        private DateTime _date=new(2026,10,28,0,0,0,DateTimeKind.Utc);
        private DateTime? _nullable=new DateTime(2026,10,28,0,0,0,DateTimeKind.Utc);
        private DateTimeOffset _offset=new(2026,10,28,0,0,0,TimeSpan.Zero);
        private decimal _amount;
        public DateTime Date{get=>_date;set=>SetProperty(ref _date,value);}
        public DateTime? Nullable{get=>_nullable;set=>SetProperty(ref _nullable,value);}
        public DateTimeOffset Offset{get=>_offset;set=>SetProperty(ref _offset,value);}
        public decimal Amount{get=>_amount;set=>SetProperty(ref _amount,value);}
    }
    [Theory,InlineData(DateTimeKind.Unspecified),InlineData(DateTimeKind.Local),InlineData(DateTimeKind.Utc)]
    public void Entity_assignment_retains_kind_even_when_ticks_match_the_constructor_default(DateTimeKind kind)
    {
        var row=new Probe();var events=0;row.PropertyChanged+=(_,_)=>events++;
        var proposed=DateTime.SpecifyKind(row.Date,kind);row.Date=proposed;
        Assert.Equal(proposed.Ticks,row.Date.Ticks);Assert.Equal(kind,row.Date.Kind);Assert.Equal(kind==DateTimeKind.Utc?0:1,events);
        row.Date=proposed;Assert.Equal(kind==DateTimeKind.Utc?0:1,events);
    }
    [Fact]
    public void Entity_nullable_assignment_preserves_kind_and_null_changes()
    {
        var row=new Probe();var events=0;row.PropertyChanged+=(_,_)=>events++;
        row.Nullable=DateTime.SpecifyKind(row.Nullable!.Value,DateTimeKind.Unspecified);Assert.Equal(DateTimeKind.Unspecified,row.Nullable.Value.Kind);Assert.Equal(1,events);
        row.Nullable=null;row.Nullable=null;Assert.Null(row.Nullable);Assert.Equal(2,events);
    }
    [Fact]
    public void Entity_offset_assignment_preserves_the_explicit_offset_for_the_same_instant()
    {
        var row=new Probe();var events=0;row.PropertyChanged+=(_,_)=>events++;var proposed=row.Offset.ToOffset(TimeSpan.FromHours(3));
        row.Offset=proposed;Assert.True(proposed.EqualsExact(row.Offset));Assert.Equal(1,events);
        row.Offset=proposed;Assert.Equal(1,events);
    }
    [Fact]
    public void Ordinary_numeric_assignment_keeps_existing_decimal_value_equality()
    {
        var row=new Probe();var events=0;row.PropertyChanged+=(_,_)=>events++;row.Amount=1.00m;row.Amount=1.000m;
        Assert.Equal(1m,row.Amount);Assert.Equal(1,events);
    }
}
