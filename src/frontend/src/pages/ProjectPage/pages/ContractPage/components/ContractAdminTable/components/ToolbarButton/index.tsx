
import { Button } from '@equinor/fusion-components';
import { ReactNode, forwardRef } from 'react';
import styles from './styles.less';

type ToolbarButtonProps = {
    icon: ReactNode;
    title: string;
    onClick?: () => void;
    disabled?: boolean;
};

const ToolbarButton = forwardRef<HTMLElement, ToolbarButtonProps>(
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
