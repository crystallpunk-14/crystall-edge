from LocalizationHelper import get_logger, LogText

logger = get_logger(__name__)


EMPTY_VALUES = ("None", '{ "" }')


def _value_or_none(value: str):
    value = value.strip()
    return None if value in EMPTY_VALUES else value


def read_ftl(path: str) -> dict:

    prototypes = {}

    last_prototype = ""
    try:
        logger.debug("%s: %s", LogText.READING_DATA_FROM_FILE, path)
        with open(path, encoding="utf-8") as file:
            for line in file.readlines():
                if line.strip().startswith("#") or line.strip() == '':
                    continue

                if not line.startswith(" "): 
                    proto_id, proto_name = line.split(" = ")
                    proto_id = proto_id.replace("ent-", "")
                    last_prototype = proto_id
                    prototypes[proto_id] = {
                        "id": proto_id,
                        "name": _value_or_none(proto_name),
                        "description": None,
                        "suffix": None
                    }
                else:
                    if line.strip().startswith(".desc"):
                        attr = "description"
                    elif line.strip().startswith(".suffix"):
                        attr = "suffix"
                    else:
                        continue

                    prototypes[last_prototype][attr] = _value_or_none(line.split(" = ", 1)[1])
    except Exception as e:
        logger.error("%s: %s - %s", LogText.ERROR_WHILE_READING_DATA_FROM_FILE, path, e, exc_info=True)
    else:
        return prototypes
