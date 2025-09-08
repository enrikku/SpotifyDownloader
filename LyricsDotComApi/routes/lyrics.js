import { Router } from "express";
import { z } from "zod";
import axios from "axios";
import * as cheerio from "cheerio";


const router = Router();

const urlSchema = z
  .string()
  .url("URL inválida")
  .refine((url) => url.includes("letras.com"), "La URL debe ser de letras.com");

// GET /api/lyrics?url=...
router.get("/lyrics", async (req, res) => {
  try {
    const { url } = req.query;

    if (!url) {
      return res.status(400).json({ error: "Falta el parámetro url" });
    }

    const { data } = await axios.get(url);
    const $ = cheerio.load(data);

    let lyrics = "";

    $(".lyric-original p").each((_, el) => {
      const paragraph = $(el)
        .html()
        .replace(/<br\s*\/?>/gi, "\n"); 

      lyrics += paragraph + "\n\n";
    });

    lyrics = lyrics.replace(/<\/?[^>]+(>|$)/g, "").trim();

    res.json({ lyrics });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: "No se pudo obtener la letra" });
  }
});

export default router;
