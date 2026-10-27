using System.Collections.Generic;
using UnityEngine;

namespace OneBlock.Gameplay
{
    /// <summary>Pool of bare SpriteRenderers so streaming the level never allocates GameObjects mid-run.</summary>
    public sealed class SpritePool
    {
        readonly Transform _parent;
        readonly string _name;
        readonly Stack<SpriteRenderer> _free = new();

        public SpritePool(Transform parent, string name, int prewarm = 0)
        {
            _parent = parent;
            _name = name;
            for (int i = 0; i < prewarm; i++) Release(Create());
        }

        public SpriteRenderer Get(Sprite sprite, Vector3 position, int sortingOrder)
        {
            var sr = _free.Count > 0 ? _free.Pop() : Create();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = Color.white;
            sr.flipX = false;
            sr.enabled = true;
            var t = sr.transform;
            t.position = position;
            t.localScale = Vector3.one;
            t.localRotation = Quaternion.identity;
            sr.gameObject.SetActive(true);
            return sr;
        }

        public void Release(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
            _free.Push(sr);
        }

        SpriteRenderer Create()
        {
            var go = new GameObject(_name);
            go.transform.SetParent(_parent, false);
            return go.AddComponent<SpriteRenderer>();
        }
    }
}
