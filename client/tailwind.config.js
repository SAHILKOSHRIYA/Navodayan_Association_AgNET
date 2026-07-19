/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        // JNV institutional identity (Phase 3 §2.1)
        primary: {
          600: '#1E4A8F',
          700: '#173C74',
          800: '#122B54',
          900: '#0D1F3D',
        },
        accent: {
          500: '#F59E0B',
          600: '#D97706',
        },
        ink: {
          900: '#0F172A',
          600: '#475569',
          400: '#94A3B8',
        },
        success: '#15803D',
        warning: '#B45309',
        danger: '#B91C1C',
      },
      fontFamily: {
        heading: ['Poppins', 'system-ui', 'sans-serif'],
        body: ['Inter', 'system-ui', 'sans-serif'],
      },
      borderRadius: {
        card: '12px',
        hero: '16px',
      },
    },
  },
  plugins: [],
};
