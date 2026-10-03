using System;
using TheTechIdea.Beep.DataBase;

namespace TheTechIdea.Beep.Editor.UOWManager.Interfaces
{
    /// <summary>Optional preparation of block items without changing the live item store.</summary>
    public interface IPreparedBlockItems
    {
        IBlockItemsRegistration PrepareBlockItems(Guid formInstanceId, string blockName, IEntityStructure structure);
    }

    public interface IBlockItemsRegistration : IDisposable
    {
        /// <summary>
        /// Publishes items and the caller's owned-memory registration action together.
        /// The action must not invoke providers, helpers or observers. A thrown action
        /// restores the previous item store. Dispose removes only this lease's published items.
        /// </summary>
        void Commit(Action publishRegistration);
    }

    /// <summary>Optional subscription admission tied to a prepared registration's owned state.</summary>
    public interface IGatedUnitOfWorkEventSubscriptions : IOwnedUnitOfWorkEventSubscriptions
    {
        /// <summary>The nonthrowing predicate is evaluated outside helper ownership monitors.</summary>
        IDisposable SubscribeOwned(IUnitofWork unitOfWork, string blockName, Func<bool> canDispatch);
    }
}
