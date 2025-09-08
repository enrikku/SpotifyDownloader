import { Router } from "express";

const router = Router();

// GET / - Hello World
router.get("/", (req, res) => {
  res.json({
    message: "Hello World",
  });
});

export default router;
