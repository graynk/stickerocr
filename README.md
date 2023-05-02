# Sticker OCR bot
This is in no way final or ready, but I want to use it even in it's current state, so here it is.
The bot uses [EasyOCR](https://github.com/JaidedAI/EasyOCR) to recognize text on Telegram stickers, 
then lets you search for them.
OCR is set to detect Russian and English languages only - others might work too, though not likely.

Bot itself _would be_ available at [@sticker_ocr_bot](https://t.me/sticker_ocr_bot), but my home server is missing 
AVX2, which is required for pytorch, and by extension - EasyOCR, so... 

The editor is not yet available and won't be for some time

## Usage
To search for the sticker use it as an inline bot: just enter in the message field
> @sticker_ocr_bot request

This will search for stickers that I deemed worthy of public search (i.e. - no porn).

To search for your own stickers - message the bot directly and send them a single sticker from any pack.
This will analyse the pack completely, at the same time adding it to your own "favorites" collection, 
letting you search for stickers in this pack even if it's not in the public pool.

### Disclaimer
As I just recently revived this project and don't have enough time to work on it - it's bound to crash and/or respond 
slowly, or be unavailable for several days. It's in no way complete and does not contain any amount of good code. 
Bear with me.

## TODO list
* Rate limiter
* Host it somewhere with a dedicated video card
* Launch editor, letting users change recognized values in their packs