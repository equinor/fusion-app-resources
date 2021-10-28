
import {
    useTooltipRef,
    IconButton,
    AddIcon,
    EditIcon,
    DeleteIcon,
} from '@equinor/fusion-components';
import styles from './styles.less';
import ExcelImportIcon from '../../../../../../../../components/ExcelImportIcon';
import { FC, MouseEvent } from 'react';

export type IconButtonProps = {
    disabled?: boolean;
    iconColor?: string;
    onClick?: (e: MouseEvent<HTMLButtonElement>) => void;
};

export type ToolBarProps = {
    addButton?: IconButtonProps;
    deleteButton?: IconButtonProps;
    editButton?: IconButtonProps;
    excelImportButton?: IconButtonProps;
};

const ManagePersonnelToolBar: FC<ToolBarProps> = ({
    addButton,
    deleteButton,
    editButton,
    excelImportButton,
}) => {
    return (
        <div className={styles.container}>
            {addButton && (
                <IconButton
                    id="add-btn"
                    ref={useTooltipRef('Add new personnel')}
                    onClick={addButton?.onClick}
                    disabled={addButton?.disabled}
                >
                    <AddIcon color={addButton?.iconColor} />
                </IconButton>
            )}
            {excelImportButton && (
                <IconButton
                    id="excel-import-btn"
                    ref={useTooltipRef('Import personnel from excel file')}
                    onClick={excelImportButton?.onClick}
                    disabled={excelImportButton?.disabled}
                >
                    <ExcelImportIcon color={excelImportButton?.iconColor} />
                </IconButton>
            )}
            {deleteButton && (
                <IconButton
                    id="delete-btn"
                    ref={useTooltipRef('Delete selected personnel')}
                    onClick={deleteButton?.onClick}
                    disabled={deleteButton?.disabled}
                >
                    <DeleteIcon color={deleteButton?.iconColor} outline />
                </IconButton>
            )}
            {editButton && (
                <IconButton
                    id="edit-btn"
                    ref={useTooltipRef('Edit selected personnel')}
                    onClick={editButton?.onClick}
                    disabled={editButton?.disabled}
                >
                    <EditIcon color={editButton?.iconColor} outline />
                </IconButton>
            )}
        </div>
    );
};

export default ManagePersonnelToolBar;
