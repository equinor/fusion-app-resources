import * as styles from './styles.less';
import {
    IconButton,
    AddIcon,
    useTooltipRef,
    CloseIcon,
    CopyIcon,
} from '@equinor/fusion-components';

type TaskbarProps<T> = {
    onAddItem: () => void;
    onRemoveItems: (removeItems: T[]) => void;
    onCopyItems: (copyItems: T[]) => void;
    selectedItems: T[];
};

function Taskbar<T>({ onAddItem, onRemoveItems, onCopyItems, selectedItems }: TaskbarProps<T>) {
    const addItemTooltipRef = useTooltipRef('Add item');
    const removeItemTooltipRef = useTooltipRef('Remove selected item(s)');
    const copyTooltipRef = useTooltipRef('Copy selected item(s)');

    return (
        <div className={styles.taskBar}>
            <IconButton onClick={onAddItem} ref={addItemTooltipRef}>
                <AddIcon />
            </IconButton>
            <IconButton
                ref={copyTooltipRef}
                disabled={selectedItems.length <= 0}
                onClick={() => onCopyItems(selectedItems)}
            >
                <CopyIcon />
            </IconButton>
            <IconButton
                ref={removeItemTooltipRef}
                disabled={selectedItems.length <= 0}
                onClick={() => onRemoveItems(selectedItems)}
            >
                <CloseIcon />
            </IconButton>
        </div>
    );
}

export default Taskbar;
