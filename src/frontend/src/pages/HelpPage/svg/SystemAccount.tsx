import * as React from 'react';

const SystemAccount: React.FC = () => {
    return (
        <svg
            width="28"
            height="33"
            viewBox="0 0 28 33"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
        >
            <g filter="url(#filter0_dd)">
                <path
                    d="M0 14C0 7.37258 5.37258 2 12 2V2C18.6274 2 24 7.37258 24 14V14C24 20.6274 18.6274 26 12 26V26C5.37258 26 0 20.6274 0 14V14Z"
                    fill="white"
                />
                <path
                    d="M20.5279 13.4696L15.5291 7.85366L15.5291 11.5949L17.6699 14L15.5291 16.4051L15.5291 20.1463L20.5279 14.5304C20.7887 14.2375 20.7887 13.7625 20.5279 13.4696Z"
                    fill="url(#paint0_linear)"
                />
                <path
                    d="M3.47209 14.5304L8.47094 20.1463L8.47094 16.4051L6.33014 14L8.47094 11.5949L8.47094 7.85366L3.47209 13.4696C3.21136 13.7625 3.21136 14.2375 3.47209 14.5304Z"
                    fill="url(#paint1_linear)"
                />
                <path
                    d="M9.0206 7.85376L9.0206 11.5763L10.5922 13.6811L11.994 11.8272L9.0206 7.85376Z"
                    fill="#990025"
                />
                <path
                    d="M15.4121 16.3949L15.4121 20.1363L12.4362 16.1508L13.8322 14.2836L15.4121 16.3949Z"
                    fill="#990025"
                />
                <path
                    d="M15.2 7.85919L8.80846 16.3625L8.80846 20.1417L15.2 11.5817L15.2 7.85919Z"
                    fill="#FF1243"
                />
            </g>
            <defs>
                <filter
                    id="filter0_dd"
                    x="-4"
                    y="0"
                    width="32"
                    height="33"
                    filterUnits="userSpaceOnUse"
                    colorInterpolationFilters="sRGB"
                >
                    <feFlood flood-opacity="0" result="BackgroundImageFix" />
                    <feColorMatrix
                        in="SourceAlpha"
                        type="matrix"
                        values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"
                    />
                    <feOffset dy="3" />
                    <feGaussianBlur stdDeviation="2" />
                    <feColorMatrix
                        type="matrix"
                        values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.12 0"
                    />
                    <feBlend mode="normal" in2="BackgroundImageFix" result="effect1_dropShadow" />
                    <feColorMatrix
                        in="SourceAlpha"
                        type="matrix"
                        values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 127 0"
                    />
                    <feOffset dy="2" />
                    <feGaussianBlur stdDeviation="2" />
                    <feColorMatrix
                        type="matrix"
                        values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.14 0"
                    />
                    <feBlend mode="normal" in2="effect1_dropShadow" result="effect2_dropShadow" />
                    <feBlend
                        mode="normal"
                        in="SourceGraphic"
                        in2="effect2_dropShadow"
                        result="shape"
                    />
                </filter>
                <linearGradient
                    id="paint0_linear"
                    x1="18.2646"
                    y1="17.0732"
                    x2="18.4148"
                    y2="11.0656"
                    gradientUnits="userSpaceOnUse"
                >
                    <stop offset="0.508287" stop-color="#DC002E" />
                    <stop offset="0.508387" stop-color="#FF1243" />
                </linearGradient>
                <linearGradient
                    id="paint1_linear"
                    x1="5.73547"
                    y1="10.9268"
                    x2="5.58523"
                    y2="16.9344"
                    gradientUnits="userSpaceOnUse"
                >
                    <stop offset="0.508287" stop-color="#DC002E" />
                    <stop offset="0.508387" stop-color="#FF1243" />
                </linearGradient>
            </defs>
        </svg>
    );
};

export default SystemAccount;
