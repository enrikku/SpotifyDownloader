import { fetch } from "undici";
import * as cheerio from "cheerio";

const clean = (s) =>
  s
    .replace(/\r/g, "")
    .split("\n")
    .map((x) => x.replace(/\u00A0/g, " ").trim())
    .join("\n")
    .replace(/\n{3,}/g, "\n\n")
    .trim();

function extractMetadata($) {
  const title =
    $("h1").first().text().trim() ||
    $(".cnt-head_title").text().trim() ||
    $("title").text().trim();

  const artist =
    $(".cnt-head_meta a").first().text().trim() ||
    $(".cnt-head_meta").text().trim() ||
    $("h2 a").first().text().trim();

  return {
    title: title || null,
    artist: artist || null,
  };
}

function extractLyrics($) {
  const container = $(".lyric-original").first();
  if (container.length) {
    container.find(".viewFractions").remove();
    container.find("br").replaceWith("\n");
    const ps = container.find("p");
    let text;
    if (ps.length) {
      text = ps
        .map((_, el) => $(el).text())
        .get()
        .join("\n\n");
    } else {
      text = container.text();
    }
    text = clean(text);
    if (text && text.length > 20) return text;
  }

  const candidates = [
    ".cnt-letra p",
    ".letra p",
    ".letra",
    ".lyric p",
    ".lyric",
    'div[itemprop="description"] p',
    "article .content p",
    "article p",
  ];

  for (const sel of candidates) {
    const nodes = $(sel);
    if (nodes.length) {
      const text = clean(
        nodes
          .map((_, el) => $(el).text())
          .get()
          .join("\n\n")
      );
      if (text && text.length > 50) return text;
    }
  }

  let best = "";
  $("main, article, body")
    .find("*")
    .each((_, el) => {
      const t = clean($(el).text());
      if (t.split("\n").length > 5 && t.length > best.length) best = t;
    });
  return best || null;
}

export async function scrapeLyrics(url) {
  const resp = await fetch(url, {
    headers: {
      "User-Agent": "Mozilla/5.0 (compatible; LetrasScraper/1.0)",
      Accept: "text/html,application/xhtml+xml",
    },
  });

  if (!resp.ok) {
    return {
      ok: false,
      error: `Error al obtener la página: ${resp.status}`,
    };
  }

  const html = await resp.text();
  const $ = cheerio.load(html);

  const lyrics = extractLyrics($);

  return {
    ok: true,
    lyrics,
  };
}
