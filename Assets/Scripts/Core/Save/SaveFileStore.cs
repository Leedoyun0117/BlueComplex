using System;
using System.IO;

namespace BlueComplex.Core.Save
{
    /// <summary>
    /// 세이브 파일 하나를 읽고 쓴다. 직렬화 방식(유니티에선 JsonUtility)은 밖에서 넘긴다 — Core는 UnityEngine을 모른다.
    ///
    /// 쓰기는 옆 임시 파일에 다 쓴 뒤 바꿔 끼운다 — 쓰는 도중 게임이 꺼져도 이전 세이브가 반쯤 쓰인 채 남지 않는다.
    /// 읽다가 깨진 파일을 만나면 ".corrupt"로 옆에 치워 두고 없던 것으로 친다 — 다음 저장이 덮어써도 원본은 남는다.
    /// </summary>
    public sealed class SaveFileStore
    {
        private readonly Func<SaveData, string> _serialize;
        private readonly Func<string, SaveData> _deserialize;

        public string Path { get; }

        public SaveFileStore(string path, Func<SaveData, string> serialize, Func<string, SaveData> deserialize)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            _serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
        }

        public bool Exists => File.Exists(Path);

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var temp = Path + ".tmp";
            File.WriteAllText(temp, _serialize(data));
            if (File.Exists(Path)) File.Replace(temp, Path, null);
            else File.Move(temp, Path);
        }

        /// <summary>파일이 있고 읽을 수 있으면 true. 없으면 false(<paramref name="error"/>는 null). 깨졌으면 false와 사유 —
        /// 그 파일은 <c>.corrupt</c>로 옮겨 둔다.</summary>
        public bool TryLoad(out SaveData data, out string error)
        {
            data = null;
            error = null;
            if (!File.Exists(Path)) return false;

            try
            {
                data = _deserialize(File.ReadAllText(Path));
                if (data == null) throw new InvalidDataException("빈 세이브");
                data.Clues ??= new System.Collections.Generic.List<ClueSaveEntry>();
                return true;
            }
            catch (Exception e)
            {
                data = null;
                error = e.Message;
                var aside = Path + ".corrupt";
                try
                {
                    if (File.Exists(aside)) File.Delete(aside);
                    File.Move(Path, aside);
                }
                catch (IOException) { /* 치우지 못해도 없던 것으로 친다 */ }
                return false;
            }
        }

        public void Delete()
        {
            if (File.Exists(Path)) File.Delete(Path);
        }
    }
}
