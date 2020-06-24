import * as React from 'react';

import * as styles from './styles.less';

type ScrollUpFabProps = {
    onClick: () => void;
};

const ScrollUpFab: React.FC<ScrollUpFabProps> = ({ onClick, children }) => {
    return (
        <button
            className={styles.container}
            onMouseDown={(e) => e.preventDefault()}
            onClick={onClick}
        >
            <span className={styles.iconContainer}>{children}</span>
        </button>
    );
};

export default ScrollUpFab;
