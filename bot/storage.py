import itertools
import os
from typing import List, Tuple

import psycopg2
from psycopg2.extensions import cursor


class Storage:
    conn = psycopg2.connect('host=%s dbname=stickerocr user=bot password=%s' % (os.environ['DB_URL'],
                                                                                os.environ['DB_PASSWORD']))

    def create_schema(self):
        with self.conn:
            with self.conn.cursor() as cur:
                cur.execute("""
                create table if not exists users (
                    user_id    bigint not null primary key, 
                    use_public boolean    default true,
                    locale     varchar(2) default 'ru'
                );""")
                cur.execute("""
                create table if not exists stickers
                (
                    unique_id     text not null primary key,
                    id            text,
                    order_in_pack integer,
                    set_name      text,
                    text          text,
                    text_index    tsvector generated always as ((to_tsvector('russian'::regconfig, COALESCE(text, ''::text)) ||
                                                            to_tsvector('english'::regconfig, COALESCE(text, ''::text)))) stored,
                    image         bytea
                );""")
                cur.execute("create index if not exists set_index on stickers(set_name asc);")
                cur.execute(
                    "create table if not exists banned_stickers (unique_id text not null primary key references stickers(unique_id));")
                cur.execute(
                    "create table if not exists unprocessed_stickers (unique_id text not null primary key references stickers(unique_id));")
                cur.execute("""
                create table if not exists sticker_sets (
                    name     text not null primary key,
                    title    text,
                    thumb    bytea,
                    approved boolean default false
                );""")
                cur.execute("""
                create table if not exists user_edits (
                    unique_id  text   not null references stickers(unique_id),
                    user_id    bigint not null references users(user_id),
                    text       text   not null,
                    text_index tsvector generated always as ((to_tsvector('russian'::regconfig, COALESCE(text, ''::text)) ||
                                                      to_tsvector('english'::regconfig, COALESCE(text, ''::text)))) stored,
                    rejected   boolean default false,
                    constraint user_edits_pkey primary key (unique_id, user_id)
                );""")
                cur.execute("""
                create table if not exists favorites(
                    user_id  bigint not null references users(user_id),
                    set_name text   not null references sticker_sets(name),
                    constraint composite_pk primary key (user_id, set_name)
                );""")

    def find_stickers(self, query: str, user_id: int, limit: int) -> List[Tuple[str, str]]:
        with self.conn:
            with self.conn.cursor() as cur:
                upsert_user(cur, user_id)
                if query == '':
                    fetch_default_response(cur, user_id, limit)
                elif not check_word(query):
                    search_via_tsquery(cur, query, user_id, limit)
                else:
                    search_via_websearch(cur, query, user_id, limit)
                return cur.fetchall()

    def save_set_info(self, user_id: int, set_name: str, title: str, thumb: bytes) -> List[Tuple[str]]:
        with self.conn:
            with self.conn.cursor() as cur:
                upsert_user(cur, user_id)
                upsert_sticker_set(cur, set_name, title, thumb)
                upsert_favorites(cur, user_id, set_name)
                return fetch_set_ids(cur, set_name)

    def save_sticker(self, sticker_id: str, sticker_unique_id: str, order: int, set_name: str, text: str,
                     body: bytes) -> None:
        with self.conn:
            with self.conn.cursor() as cur:
                cur.execute('''
                        insert into stickers (id, unique_id, order_in_pack, set_name, text, image)
                        values (%(id)s, %(unique_id)s, %(order)s, %(set)s, %(text)s, %(sticker)s)
                        on conflict (unique_id) 
                        do update set id = %(id)s, text = %(text)s, image = %(sticker)s;
                        ''',
                            {
                                'id': sticker_id,
                                'unique_id': sticker_unique_id,
                                'order': order,
                                'set': set_name,
                                'text': text,
                                'sticker': body
                            })
                cur.execute('''
                insert into unprocessed_stickers (unique_id)
                values (%s)
                on conflict (unique_id) 
                do nothing;
                ''', (sticker_unique_id,))

    def delete_stickers(self, ids: List[str]):
        with self.conn:
            with self.conn.cursor() as cur:
                cur.execute('delete from stickers where unique_id = any(%s)', (ids,))


def upsert_user(cur: cursor, user_id: int) -> None:
    cur.execute('insert into users (user_id) values (%s) on conflict (user_id) do nothing;', (user_id,))


def upsert_sticker_set(cur: cursor, set_name: str, title: str, thumb: bytes) -> None:
    cur.execute('''
    insert into sticker_sets (name, title, thumb)
    values (%(set)s, %(title)s, %(thumb)s)
    on conflict (name) do update set title = %(title)s, thumb = %(thumb)s;
    ''', {'set': set_name, 'title': title, 'thumb': thumb})


def upsert_favorites(cur: cursor, user_id: int, set_name: str) -> None:
    cur.execute('insert into favorites (user_id, set_name) values (%s, %s) on conflict (user_id, set_name) do nothing;',
                (user_id, set_name))


def fetch_set_ids(cur: cursor, set_name: str) -> List[Tuple[str]]:
    cur.execute('select unique_id from stickers where set_name = %s;', (set_name,))
    return cur.fetchall()


def fetch_default_response(cur: cursor, user_id: int, limit: int) -> None:
    cur.execute('''
    select id, unique_id
    from (
        select id, unique_id, set_name, row_number() over (partition by set_name) as row
        from stickers
        where (
            (
                (select use_public from USERS where user_id = %(user_id)s) = true
                and not exists (
                    select 1
                    from unprocessed_stickers
                    where unprocessed_stickers.unique_id = stickers.unique_id
                )
            )
            or
            set_name in (select set_name from favorites where user_id = %(user_id)s)
        ) and not exists (
            select 1
            from banned_stickers
            where banned_stickers.unique_id = stickers.unique_id
        )
    )
    as stckrs
    where row < 4
    limit %(limit)s
    ''', {'user_id': user_id, 'limit': limit})


def search_via_tsquery(cur: cursor, query: str, user_id: int, limit: int) -> None:
    query = query.replace(' ', '<->')
    cur.execute('''
    select id, unique_id from (
        select id, unique_id, set_name, row_number() over (partition by set_name) as row 
        from stickers
        where (
            (
                (select use_public from USERS where user_id = %(user_id)s) = true
                and not exists (
                    select 1
                    from unprocessed_stickers
                    where unprocessed_stickers.unique_id = stickers.unique_id
                )
            )
            or 
            set_name in (select set_name from favorites where user_id = %(user_id)s)
        ) and not exists (
            select 1 
            from banned_stickers where banned_stickers.unique_id = stickers.unique_id
        ) 
        and text_index @@ (to_tsquery('russian', %(query)s || ':*') || to_tsquery('english', %(query)s || ':*'))
    )
    as stckrs 
    where row < 4 
    limit %(limit)s
    ''', {'user_id': user_id, 'query': query, 'limit': limit})


def search_via_websearch(cur: cursor, query: str, user_id: int, limit: int) -> None:
    cur.execute('''
    select id, unique_id from (
        select id, unique_id, set_name, row_number() over (partition by set_name) as row 
        from stickers
        where (
            (
                (select use_public from USERS where user_id = %(user_id)s) = true
                and not exists (
                    select 1
                    from unprocessed_stickers
                    where unprocessed_stickers.unique_id = stickers.unique_id
                )
            ) 
            or 
            set_name in (select set_name from favorites where user_id = %(user_id)s)
        ) and not exists (
            select 1 
            from banned_stickers where banned_stickers.unique_id = stickers.unique_id
        ) 
        and text_index @@ (websearch_to_tsquery('russian', %(query)s || ':*') || websearch_to_tsquery('english', %(query)s || ':*'))
    )
    as stckrs 
    where row < 4 
    limit %(limit)s
    ''', {'user_id': user_id, 'query': query, 'limit': limit})


def check_word(word: str) -> bool:
    spec_symbols = ':/*?&'

    match = [l in spec_symbols for l in word]
    group = [k for k, g in itertools.groupby(match)]

    return sum(group) >= 1
