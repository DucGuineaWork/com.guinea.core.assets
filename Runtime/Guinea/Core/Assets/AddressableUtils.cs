using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace Guinea.Core.Assets
{
    public static partial class AddressableUtils
    {
        public static async UniTask LoadContentCatalogUpdateAsync(string catalogPath, string providerSuffix = null, bool update = false)
        {
            await Addressables.LoadContentCatalogAsync(catalogPath, true, providerSuffix).ToUniTask();
            if (update)
            {
                var handle = Addressables.UpdateCatalogs();
                await handle.ToUniTask();
                Addressables.Release(handle);
            }
        }

        public static async UniTask<AddressableAsset<T>> LoadAssetAsync<T>(object key)
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            T instance = await handle.Task;
            return new AddressableAsset<T>(handle, instance);
        }

        public static async UniTask<AddressableAsset<IList<T>>> LoadAssetsAsync<T>(IEnumerable keys, Addressables.MergeMode mode = default)
        {
            AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(keys, null, mode: mode);
            IList<T> instance = await handle.Task;
            return new AddressableAsset<IList<T>>(handle, instance);
        }

        public static UniTask UnloadUnusedAssets() => Resources.UnloadUnusedAssets().ToUniTask();
    }

    public class AddressableAsset<T>
    {
        public readonly AsyncOperationHandle handle;
        public readonly T value;

        public AddressableAsset(AsyncOperationHandle handle, T value)
        {
            this.handle = handle;
            this.value = value;
        }

        ~AddressableAsset()
        {
            Addressables.Release(handle);
        }
    }

   
}