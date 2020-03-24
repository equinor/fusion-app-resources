import * as React from 'react';
import {
    useTooltipRef,
    IconButton,
    AddIcon,
    EditIcon,
    DeleteIcon,
} from '@equinor/fusion-components';
import * as styles from './styles.less';

export type IconButtonProps = {
    disabled?: boolean;
    iconColor?: string;
    onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
};

export type ToolBarProps = {
    addButton?: IconButtonProps;
    deleteButton?: IconButtonProps;
    editButton?: IconButtonProps;
};

const ManagePersonnelToolBar: React.FC<ToolBarProps> = ({
    addButton,
    deleteButton,
    editButton,
}) => {
    return (
        <div className={styles.container}>
            {addButton && (
                <IconButton
                    ref={useTooltipRef('Add new personnel')}
                    onClick={addButton?.onClick}
                    disabled={addButton?.disabled}
                >
                    <AddIcon color={addButton?.iconColor} />
                </IconButton>
            )}
            {deleteButton && (
                <IconButton
                    ref={useTooltipRef('Delete selected personnel')}
                    onClick={deleteButton?.onClick}
                    disabled={deleteButton?.disabled}
                >
                    <DeleteIcon color={deleteButton?.iconColor} />
                </IconButton>
            )}
            {editButton && (
                <IconButton
                    ref={useTooltipRef('Edit selected personnel')}
                    onClick={editButton?.onClick}
                    disabled={editButton?.disabled}
                >
                    <EditIcon color={editButton?.iconColor} />
                </IconButton>
            )}
        </div>
    );
};

export default ManagePersonnelToolBar;
