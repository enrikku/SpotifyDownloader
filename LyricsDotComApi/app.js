// app.js
import express from "express";
import morgan from "morgan";
import cors from "cors";
import lyricsRouter from "./routes/lyrics.js";
import indexRouter from "./routes/index.js";

const app = express();

app.use(morgan("dev"));
app.use(cors());
app.use(express.json());

app.use("/", indexRouter);
app.use("/api", lyricsRouter);

app.use((req, res) => res.status(404).json({ ok: false, error: "Not Found" }));

app.use((err, req, res, next) => {
  console.error("Error no manejado:", err);

  if (err.name === "ZodError") {
    return res.status(400).json({
      ok: false,
      error: "Error de validación",
      details: err.errors.map((e) => e.message).join(", "),
    });
  }

  if (err.code === "ERR_INVALID_URL") {
    return res.status(400).json({
      ok: false,
      error: "URL inválida",
      details: err.message,
    });
  }

  if (err.code === "ENOTFOUND" || err.code === "ECONNREFUSED") {
    return res.status(503).json({
      ok: false,
      error: "Error de conexión",
      details: "No se pudo conectar al servidor de letras.com",
    });
  }

  res.status(err.status || 500).json({
    ok: false,
    error: err.message || "Error interno del servidor",
    ...(process.env.NODE_ENV === "development" && { stack: err.stack }),
  });
});

export default app;
