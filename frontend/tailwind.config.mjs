/** @type {import('tailwindcss').Config} */
export default {
  content: ['./src/**/*.{astro,html,js,jsx,md,mdx,svelte,ts,tsx,vue}'],
  theme: {
    extend: {
      colors: {
        paper: {
          light: '#ffffff',
          DEFAULT: '#fcfbf8',
          subtle: '#f5f3ec',
          border: '#10100e',
        },
        ink: {
          DEFAULT: '#10100e',
          light: '#374151',
          muted: '#6b7280',
        },
        acid: {
          DEFAULT: '#d9f99d', // Acid lime
          yellow: '#fef08a', // Highlighter yellow
          cyan: '#a5f3fc',
        },
        brand: {
          DEFAULT: '#4f46e5',
          dark: '#3730a3',
          light: '#e0e7ff',
        },
      },
      fontFamily: {
        sans: [
          'Plus Jakarta Sans',
          'Inter',
          '-apple-system',
          'BlinkMacSystemFont',
          'sans-serif',
        ],
        display: [
          'Plus Jakarta Sans',
          'Inter',
          '-apple-system',
          'sans-serif',
        ],
      },
      boxShadow: {
        'brutal': '4px 4px 0px 0px #10100e',
        'brutal-lg': '6px 6px 0px 0px #10100e',
        'brutal-xl': '8px 8px 0px 0px #10100e',
        'brutal-sm': '2px 2px 0px 0px #10100e',
        'brutal-hover': '2px 2px 0px 0px #10100e',
      },
    },
  },
  plugins: [],
};
