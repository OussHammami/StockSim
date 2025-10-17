import { useEffect, useState } from "react";

function getInitialTheme(): "light" | "dark" {
  const saved = localStorage.getItem("theme");
  if (saved === "light" || saved === "dark") return saved;
  return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function ThemeToggle() {
  const [theme, setTheme] = useState<"light" | "dark">(getInitialTheme);

  useEffect(() => {
    document.documentElement.dataset.theme = theme;
    localStorage.setItem("theme", theme);
  }, [theme]);

  return (
    <button className="btn outline" onClick={() => setTheme(theme === "light" ? "dark" : "light")} aria-label="Toggle theme">
      {theme === "light" ? "Dark" : "Light"} mode
    </button>
  );
}