import * as React from 'react';
import { Button } from '@equinor/fusion-components';
import * as styles from './styles.less';

type ToolbarButtonProps = {
    icon: React.ReactNode;
    title: string;
    onClick?: () => void;
    disabled?: boolean;
};

const ToolbarButton = React.forwardRef<HTMLElement, ToolbarButtonProps>(
    ({ icon, title, onClick, disabled }, ref) => (
        <Button frameless onClick={onClick} ref={ref} disabled={!!disabled}>
            <div className={styles.toolbarButton}>
                {icon}
                <span>{title}</span>
            </div>
        </Button>
    )
);
export default ToolbarButton;
