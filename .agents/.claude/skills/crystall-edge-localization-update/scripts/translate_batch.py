"""
Batch helper for translating entities.ftl in portions (used by the /crystall-edge-localization-update command).

    python translate_batch.py next [N]        write and print the next N untranslated fields (default 100)
    python translate_batch.py apply FILE      apply `Id.name = text` / `Id.desc = text` lines to entities.ftl
    python translate_batch.py check           sanity checks of entities.ftl
    python translate_batch.py sync-result [--dry-run]
                                              overwrite last_launch_result/result.json with the current YAML state

Work files (batch.txt, tm_hits.txt) go to <temp>/ce_loc_batch/.
"""
import json
import os
import re
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
TOOL = os.path.join(ROOT, "Tools", "_CE", "LocalizationHelper")
FTL = os.path.join(ROOT, "Resources", "Locale", "ru-RU", "_CE", "_PROTO", "entities.ftl")
RESULT = os.path.join(TOOL, "last_launch_result", "result.json")
WORK = os.path.join(tempfile.gettempdir(), "ce_loc_batch")

CYR = re.compile(r"[а-яА-ЯёЁ]")
REF = re.compile(r"^\{+\s*ent-[^}]*\}+$")
TODO = re.compile(r"(?i)^todo\b")
# Fields that are deliberately left in English (agreed with the user): essence names, dice
SKIP_KEYS = re.compile(r"^(CEEssence\w+\.name|CEd\d+Dice\.name)$")
YAML_FIELD = {"name": "name", "desc": "description"}


def load():
    raw = open(FTL, encoding="utf-8", newline="").read()
    eol = "\r\n" if "\r\n" in raw else "\n"
    return raw.split(eol), eol


def fields(lines):
    """Yield (key, line_index, value) for name and .desc lines. key is 'Id.name' or 'Id.desc'."""
    cur = None
    for i, line in enumerate(lines):
        m = re.match(r"^ent-(\S+) = (.*)$", line)
        if m:
            cur = m.group(1)
            yield f"{cur}.name", i, m.group(2)
            continue
        m = re.match(r"^\s+\.desc = (.*)$", line)
        if m and cur:
            yield f"{cur}.desc", i, m.group(1)


def needs_translation(key, value):
    v = value.strip()
    if not v or v == '{ "" }' or REF.match(v) or CYR.search(v):
        return False
    if not re.search(r"[A-Za-z]", v) or TODO.match(v) or SKIP_KEYS.match(key):
        return False
    return True


def compute():
    """Return (pending, tm_hits) lists of 'key = text' strings."""
    lines, _ = load()
    yaml = json.load(open(RESULT, encoding="utf-8"))
    # translation memory: english source (from YAML) -> already translated russian text
    tm = {}
    for key, _, v in fields(lines):
        pid, f = key.rsplit(".", 1)
        src = ((yaml.get(pid) or {}).get(YAML_FIELD[f]) or "").strip()
        if src and not TODO.match(src) and CYR.search(v) and "{" not in v:
            tm.setdefault(src, v.strip())
    pending, hits = [], []
    for key, _, v in fields(lines):
        if not needs_translation(key, v):
            continue
        src = v.strip()
        (hits if src in tm else pending).append(f"{key} = {tm.get(src, src)}")
    return pending, hits


def cmd_next(n):
    pending, hits = compute()
    os.makedirs(WORK, exist_ok=True)
    batch = pending[:n]
    open(os.path.join(WORK, "batch.txt"), "w", encoding="utf-8").write("\n".join(batch) + "\n")
    open(os.path.join(WORK, "tm_hits.txt"), "w", encoding="utf-8").write("\n".join(hits) + "\n")
    print(f"# pending {len(pending)}, in this batch {len(batch)}, exact-match hits from translation memory {len(hits)}")
    print(f"# work dir: {WORK}  (tm_hits.txt: review, then apply it like a normal batch)")
    print("\n".join(batch))


def cmd_apply(path):
    text = open(path, encoding="utf-8", errors="replace").read()
    if "�" in text:
        sys.exit("corrupted characters (U+FFFD) in " + path)
    tr = {}
    for line in text.splitlines():
        if " = " in line:
            k, v = line.split(" = ", 1)
            tr[k.strip()] = v.strip()
    lines, eol = load()
    done = set()
    for key, i, _ in fields(lines):
        if key in tr:
            lines[i] = f"ent-{key[:-5]} = {tr[key]}" if key.endswith(".name") else f"    .desc = {tr[key]}"
            done.add(key)
    open(FTL, "w", encoding="utf-8", newline="").write(eol.join(lines))
    print(f"applied {len(done)} of {len(tr)}")
    for k in sorted(set(tr) - done):
        print("NOT FOUND:", k)


def cmd_check():
    s = open(FTL, encoding="utf-8", newline="").read()
    keys = set(re.findall(r"^(ent-\S+) =", s, re.M))
    dangling = {m.group(1) for m in re.finditer(r"\{+\s*(ent-[\w-]+)(?:\.\w+)?\s*\}+", s) if m.group(1) not in keys}
    no_desc = len(re.findall(r"^ent-\S+ = .*\r?\n(?!\s+\.desc)", s, re.M))
    print(f"entries {len(keys)} | dangling refs {len(dangling)} | U+FFFD {s.count(chr(0xFFFD))} "
          f"| '= None' {s.count('= None')} | entries without .desc {no_desc}")
    for d in sorted(dangling)[:20]:
        print("dangling:", d)
    lines, _ = load()
    left = [k for k, _, v in fields(lines) if needs_translation(k, v)]
    todo = [k for k, _, v in fields(lines) if TODO.match(v.strip())]
    print(f"still untranslated {len(left)} | TODO placeholders left as is {len(todo)}")


def cmd_sync_result(dry):
    """Same thing the tool does at the end of a run: store the parsed YAML as the new 'last launch' state."""
    os.chdir(TOOL)
    sys.path.insert(0, TOOL)
    from LocalizationHelper.parsers import YamlParser
    yaml_protos = YamlParser().get_prototypes("../../../Resources/Prototypes/_CE/")
    new = {pid: obj.attrs_dict for pid, obj in yaml_protos.items()}
    old = json.load(open(RESULT, encoding="utf-8"))
    changed = [k for k in new if old.get(k) != new[k]]
    removed = [k for k in old if k not in new]
    print(f"result.json: {len(changed)} entities differ from YAML, {len(removed)} removed")
    for k in changed[:15]:
        print("  changed:", k)
    if dry:
        return
    raw = open(RESULT, encoding="utf-8", newline="").read()
    eol = "\r\n" if "\r\n" in raw else "\n"
    out = json.dumps(new, ensure_ascii=False, indent=4).replace("\n", eol)
    open(RESULT, "w", encoding="utf-8", newline="").write(out)
    print("result.json updated")


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    a = sys.argv[1:]
    if not a:
        sys.exit(__doc__)
    if a[0] == "next":
        cmd_next(int(a[1]) if len(a) > 1 else 100)
    elif a[0] == "apply":
        cmd_apply(a[1])
    elif a[0] == "check":
        cmd_check()
    elif a[0] == "sync-result":
        cmd_sync_result("--dry-run" in a)
    else:
        sys.exit(__doc__)
