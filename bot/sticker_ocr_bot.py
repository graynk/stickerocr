#!/usr/bin/env python
# -*- coding: utf-8 -*-
import os
from concurrent.futures import ThreadPoolExecutor
from typing import List

import easyocr
from telegram import InlineQueryResultCachedSticker
from telegram import Update
from telegram import constants
from telegram.ext import Application
from telegram.ext import ContextTypes
from telegram.ext import CommandHandler
from telegram.ext import filters
from telegram.ext import InlineQueryHandler
from telegram.ext import MessageHandler
from telegram.ext import Defaults

from sticker import PreparedSticker
from storage import Storage

reader = easyocr.Reader(['ru', 'en'])
storage = Storage()
executor = ThreadPoolExecutor(max_workers=3)


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await context.bot.send_message(chat_id=update.effective_chat.id, text='Привет!\nОтправь мне любой стикер из '
                                                                          'пака, и я просканирую весь пак. '
                                                                          'После этого по этому паку можно будет искать'
                                                                          'в inline-режиме.')


async def find_sticker(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    user = update.inline_query.from_user
    query = update.inline_query.query
    if query != '':
        print(user.first_name)
        print(query, flush=True)
    results = []
    stickers = storage.find_stickers(query, user.id, constants.InlineQueryLimit.RESULTS)

    for sticker_id in stickers:
        results.append(
            InlineQueryResultCachedSticker(
                id=sticker_id[1],
                sticker_file_id=sticker_id[0])
        )
    await update.inline_query.answer(results,
                                     cache_time=0,
                                     switch_pm_text='Блин блинский, хочу свои стикеры тоже',
                                     switch_pm_parameter='123')


async def analyze_pack(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    user_id = update.effective_message.from_user.id
    set_name = update.effective_message.sticker.set_name
    if set_name == '' or set_name is None:
        await update.effective_message.reply_text('Стикеры без пака пока не умею сохранять')
        return
    sticker_set = await context.bot.get_sticker_set(set_name)
    await update.effective_message.reply_text('Подождите, идет обработка стикеров...', quote=True)

    set_name = sticker_set.name
    sticker_set_thumb = sticker_set.thumbnail if sticker_set.thumbnail else sticker_set.stickers[0].thumbnail
    sticker_set_thumb_file = await sticker_set_thumb.get_file()
    sticker_thumb_bytes = await sticker_set_thumb_file.download_as_bytearray()
    saved_sticker_id_tuples = storage.save_set_info(user_id, set_name, sticker_set.title, sticker_thumb_bytes)
    saved_sticker_unique_ids = [id_tuple[0] for id_tuple in saved_sticker_id_tuples]
    unique_ids_in_set = [sticker.file_unique_id for sticker in sticker_set.stickers]
    not_recognized = [sticker_id for sticker_id in unique_ids_in_set if
                      sticker_id not in saved_sticker_unique_ids]
    stickers_to_recognize = [sticker for sticker in sticker_set.stickers if
                             sticker.file_unique_id in not_recognized]
    ids_to_delete = [sticker_id for sticker_id in saved_sticker_unique_ids if
                     sticker_id not in unique_ids_in_set]

    prepared_stickers: List[PreparedSticker] = []

    for index, sticker in enumerate(stickers_to_recognize):
        sticker_file = await sticker.get_file()
        sticker_bytes = await sticker_file.download_as_bytearray(pool_timeout=60)
        prepared_stickers.append(PreparedSticker(file_id=sticker.file_id,
                                                 file_unique_id=sticker.file_unique_id,
                                                 index=sticker_set.stickers.index(sticker),
                                                 set_name=set_name,
                                                 sticker_bytes=bytes(sticker_bytes)))
    if len(ids_to_delete) != 0:
        storage.delete_stickers(ids_to_delete)
    job = executor.submit(analyze_pack_background_job, prepared_stickers)
    crashed = job.exception()
    # yes I write go why do you ask
    if not crashed:
        result_message = 'Готово, пак "{}" обработан'
    else:
        print(crashed)
        result_message = 'Не удалось обработать пак "{}"'
    await update.effective_message.reply_text(result_message.format(sticker_set.title), quote=True)


def analyze_pack_background_job(prepared_stickers: List[PreparedSticker]) -> None:
    for sticker in prepared_stickers:
        sticker.text = extract_and_save_text(sticker.sticker_bytes)
        storage.save_sticker(sticker)


def extract_and_save_text(sticker: bytes) -> str:
    result = reader.readtext(sticker, detail=0)
    return ' ' \
        .join(map(str.strip, result)) \
        .replace('\n', ' ')\
        .replace('\f', '') \
        .replace('\r', '') \
        .replace(':', '') \
        .strip()


async def error_callback(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    print(context.error, flush=True)


if __name__ == '__main__':
    bot_token = os.environ['BOT_TOKEN']
    storage.create_schema()
    application = Application.builder() \
        .token(bot_token) \
        .defaults(Defaults(block=False)) \
        .read_timeout(30) \
        .write_timeout(30) \
        .build()
    application.add_handler(CommandHandler(str('start'), start))
    application.add_handler(InlineQueryHandler(find_sticker))
    application.add_handler(MessageHandler(filters.Sticker.STATIC, analyze_pack))
    application.add_error_handler(error_callback)
    application.run_polling(allowed_updates=[Update.MESSAGE, Update.INLINE_QUERY])
