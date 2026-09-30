from LocalizationHelper import get_logger, LogText
from LocalizationHelper.entity import Entity

logger = get_logger(__name__)

INDENT = "    "
EMPTY_VALUE = '{ "" }'  # Fluent way to explicitly write an empty string (the engine requires .desc to exist)


def create_ftl(prototype: Entity) -> str:
    logger.debug("%s: %s", LogText.FORMING_FTL_FOR_PROTOTYPE, prototype.attrs_dict)
    ftl = ""

    ftl += f"ent-{prototype.id} = {prototype.name or EMPTY_VALUE}\n"

    ftl += f"{INDENT}.desc = {prototype.description or EMPTY_VALUE}\n"

    if prototype.suffix:
        ftl += f"{INDENT}.suffix = {prototype.suffix}\n"

    ftl += "\n"

    return ftl
