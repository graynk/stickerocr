FROM python:3.10-bullseye

WORKDIR /usr/src/app

RUN apt-get update && apt-get install -y python3-dev libpq-dev
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt
COPY sticker_ocr_bot.py ./
COPY storage.py ./
COPY sticker.py ./

CMD [ "python", "./sticker_ocr_bot.py" ]