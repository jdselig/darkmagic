using System.Collections;
using System.Reflection;
using NUnit.Framework;

namespace DarkMagic.Tests
{
    public sealed class FlowTests
    {
        [Test]
        public void SelectEntryRemainsSelectableWhenReferencePayloadIsNull()
        {
            var menu = new U.Flow.Menu("Choose").AddSelect<string>("Nothing", null);
            var entriesField = typeof(U.Flow.Menu).GetField(
                "Entries",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            var entries = (IList)entriesField.GetValue(menu);
            var entry = entries[0];
            var isSelect = (bool)entry.GetType().GetProperty("IsSelect").GetValue(entry);

            Assert.That(isSelect, Is.True);
        }
    }
}
