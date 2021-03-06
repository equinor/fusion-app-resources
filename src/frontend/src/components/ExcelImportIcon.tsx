import { IconProps, useIcon } from '@equinor/fusion-components';

const ExcelImportIcon = (props: IconProps) => {
    const iconFactory = useIcon(
        <path
            d="M14 3L2 5v14l12 2v-2h7a1 1 0 001-1V6a1 1 0 00-1-1h-7V3zm-2 2.361V18.64l-8-1.332V6.693l8-1.332zM14 7h2v2h-2V7zm4 0h2v2h-2V7zM5.176 8.297l1.885 3.697L5 15.704h1.736l1.123-2.395c.075-.23.126-.4.15-.514h.016c.041.238.091.407.133.492l1.113 2.414H11l-1.994-3.734 1.937-3.67h-1.62l-1.03 2.197c-.1.285-.167.505-.201.647h-.026a4.519 4.519 0 00-.19-.63l-.923-2.214H5.176zM14 11h2v2h-2v-2zm4 0h2v2h-2v-2zm-4 4h2v2h-2v-2zm4 0h2v2h-2v-2z"
            fill="currentColor"
        />
    );

    return iconFactory(props);
};

export default ExcelImportIcon;
