import * as React from 'react';
import * as styles from './styles.less';
import Arrow from './Arrow';
import classNames from 'classnames';

export type TooltipPlacement = 'below' | 'above' | 'left' | 'right';

type PopOverMenuProps = {
    label?: String;
    placement?: TooltipPlacement;

}
const PopOverMenu: React.FC<PopOverMenuProps> = ({
    label,
    placement = 'below',
    children
}) => {
    const [isOpen, setIsOpen] = React.useState<Boolean>(false);
    const tooltipClassName = classNames(styles.tooltip, styles[placement.toLocaleLowerCase()]);


    const open = React.useCallback(
        () => { setIsOpen(!isOpen) },
        [isOpen]
    );

    return (
        <div onClick={open} className={styles.container}>
            <span className={styles.label}>{label}</span>
            {isOpen && (
                <div onClick={() => setIsOpen(false)} className={tooltipClassName}>
                    <Arrow />
                    <span className={styles.content}>{children}</span>
                </div>
            )}
        </div>);
};

export default PopOverMenu
