from dataclasses import dataclass


@dataclass
class PreparedSticker:
    file_id: str
    file_unique_id: str
    index: int
    set_name: str
    sticker_bytes: bytes
    text: str = ''
