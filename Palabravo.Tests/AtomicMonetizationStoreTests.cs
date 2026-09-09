using Palabravo.Core.Monetization;

namespace Palabravo.Tests;

public sealed class AtomicMonetizationStoreTests
{
    [Fact]
    public void Reopening_preserves_benefits_and_ignores_an_uncommitted_temporary_file()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var first = new AtomicMonetizationStore(path);
            first.Update(s => { s.OwnsRemoveAds = true; s.SessionInterstitials = 2; s.PendingRewards.Add("reward"); return true; });
            File.WriteAllText(path + ".tmp", "{interrupted");
            var second = new AtomicMonetizationStore(path);
            Assert.True(second.Update(s => s.OwnsRemoveAds));
            Assert.Equal(2, second.Update(s => s.SessionInterstitials));
            Assert.Equal("reward", second.Update(s => s.PendingRewards.Single()));
            Assert.Throws<InvalidOperationException>(() => second.Update<bool>(s => { s.OwnsRemoveAds = false; throw new InvalidOperationException(); }));
            Assert.True(second.Update(s => s.OwnsRemoveAds));
        }
        finally { File.Delete(path); File.Delete(path + ".tmp"); }
    }
}
