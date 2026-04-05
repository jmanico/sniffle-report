export function SniffleReportLogo() {
  return (
    <svg
      aria-hidden="true"
      className="brand-mark"
      viewBox="0 0 88 88"
      xmlns="http://www.w3.org/2000/svg"
    >
      <defs>
        <linearGradient id="sniffle-report-bg" x1="14" x2="74" y1="10" y2="78" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="#fff7ed" />
          <stop offset="1" stopColor="#fed7aa" />
        </linearGradient>
        <linearGradient
          id="sniffle-report-card"
          x1="47"
          x2="70"
          y1="31"
          y2="63"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#0f766e" />
          <stop offset="1" stopColor="#155e75" />
        </linearGradient>
      </defs>

      <rect width="88" height="88" rx="24" fill="#1a1a2e" />
      <rect x="6" y="6" width="76" height="76" rx="20" fill="url(#sniffle-report-bg)" />

      <path
        d="M29 27c-6.7 0-12.5 5-13.4 11.7-.9 6.9 3.5 13.4 10.2 15.1 4 1 6.2 3 6.2 6.3 0 3.5-2.8 6.2-6.4 6.2-2.1 0-4-.8-5.4-2.3"
        fill="none"
        stroke="#c2410c"
        strokeLinecap="round"
        strokeWidth="6"
      />
      <path
        d="M25.5 22.5c3.1-4.3 7.9-6.8 13.1-6.8 5.6 0 10.8 2.8 13.9 7.4"
        fill="none"
        stroke="#c2410c"
        strokeLinecap="round"
        strokeWidth="5"
      />
      <path
        d="M21.5 37.5h12.3c4.9 0 8.8 3.9 8.8 8.8 0 4.5-3.4 8.3-7.8 8.8"
        fill="none"
        stroke="#ea580c"
        strokeLinecap="round"
        strokeWidth="4"
      />

      <rect x="45" y="25" width="25" height="38" rx="8" fill="url(#sniffle-report-card)" />
      <path d="M51 36h13" fill="none" stroke="#e0f2fe" strokeLinecap="round" strokeWidth="3" />
      <path d="M51 44h9" fill="none" stroke="#e0f2fe" strokeLinecap="round" strokeWidth="3" />
      <path d="M51 52h7" fill="none" stroke="#e0f2fe" strokeLinecap="round" strokeWidth="3" />
      <circle cx="63.5" cy="52" r="3.5" fill="#f97316" />
      <path
        d="M48 68c4.3-1.3 8.5-4.1 11.7-8 3.3 2.6 7.5 4.4 12.3 5.1"
        fill="none"
        stroke="#1a1a2e"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="4"
      />
    </svg>
  )
}
