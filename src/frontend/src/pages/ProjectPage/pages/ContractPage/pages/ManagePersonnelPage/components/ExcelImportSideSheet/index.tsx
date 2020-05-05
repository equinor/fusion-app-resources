import * as React from 'react';
import * as styles from './styles.less';
import { Spinner, OverlayPortal, Scrim, Button, ModalSideSheet } from '@equinor/fusion-components';

type ExcelImportSideSheetProps = {
    setSelectedFile: React.Dispatch<React.SetStateAction<File | null>>;
    isProccessing: boolean;
    isOpen: boolean;
    setIsOpen: React.Dispatch<React.SetStateAction<boolean>>;
};

const ExcelImportSideSheet: React.FC<ExcelImportSideSheetProps> = ({
    setSelectedFile,
    isProccessing,
    isOpen,
    setIsOpen,
}) => {
    const [selectedFileForUpload, setSelectedFileForUpload] = React.useState<File | null>(null);
    const fileInput = React.useRef<HTMLInputElement>(null);
    const [fileError, setFileError] = React.useState<string | null>(null);

    React.useEffect(() => {
        setSelectedFileForUpload(null);
        setFileError(null);
    }, [isOpen]);

    const validateAndSetFile = React.useCallback((file: File) => {
        const fileExtension = file.name.substr(file.name.lastIndexOf('.') + 1).toLowerCase();

        if (fileExtension !== 'xlsx') {
            setFileError('Fileformat not supported. Please again with a xlsx file');
            setSelectedFileForUpload(null);
            return;
        }
        setFileError(null);
        setSelectedFileForUpload(file);
    }, []);

    const dragDropFileUpload = React.useCallback((e: React.DragEvent<HTMLDivElement>) => {
        e.preventDefault();
        if (!e.dataTransfer.items || e.dataTransfer.items[0].kind !== 'file') return;

        const file = e.dataTransfer.items[0].getAsFile();
        if (!file) return;
        validateAndSetFile(file);
    }, []);

    const inputFileUpload = React.useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        e.preventDefault();

        const file = e.target.files ? e.target.files[0] : null;
        if (!file) return;
        validateAndSetFile(file);
    }, []);

    const fileInputClick = React.useCallback(() => {
        fileInput?.current?.click();
    }, [fileInput]);

    const startProcessingSelectedFile = React.useCallback(() => {
        setSelectedFile(selectedFileForUpload);
    }, [selectedFileForUpload]);

    const stopPropagationAndDefault = React.useCallback((e: React.DragEvent<HTMLDivElement>) => {
        e.stopPropagation();
        e.preventDefault();
    }, []);

    const closeSidesheet = React.useCallback(() => {
        !isProccessing && setIsOpen(false);
    }, [isProccessing]);

    return (
        <ModalSideSheet
            header="Excel Import"
            show={isOpen}
            size={'medium'}
            onClose={closeSidesheet}
        >
            <div onClick={(e) => e.stopPropagation()} className={styles.excelImportModal}>
                {isProccessing && (
                    <div className={styles.processing}>
                        <Spinner centered title="Processing Excel file" />
                    </div>
                )}
                {!isProccessing && (
                    <div className={styles.inputs}>
                        <div
                            className={styles.dragDrop}
                            onDrop={dragDropFileUpload}
                            onDragEnter={(e) => stopPropagationAndDefault(e)}
                            onDragOver={(e) => stopPropagationAndDefault(e)}
                        >
                            <div className={styles.dragDropText}>
                                <p>Drag and drop an excel file here</p>
                                <p>or </p>
                                <div className={styles.fileInput}>
                                    <div className={styles.inputButton}>
                                        <Button onClick={fileInputClick}>
                                            Select an excel file
                                        </Button>
                                    </div>
                                    <input
                                        className={styles.inputField}
                                        ref={fileInput}
                                        type="file"
                                        onChange={inputFileUpload}
                                    ></input>
                                </div>
                            </div>
                            <div className={styles.selectedFileText}>
                                {selectedFileForUpload &&
                                    !fileError &&
                                    `Selected file: ${selectedFileForUpload.name}`}
                                {fileError && fileError}
                            </div>
                        </div>
                        <div className={styles.processButton}>
                            <Button
                                disabled={!selectedFileForUpload}
                                onClick={startProcessingSelectedFile}
                            >
                                Process selected excel file
                            </Button>
                        </div>
                    </div>
                )}
            </div>
        </ModalSideSheet>
    );
};

export default ExcelImportSideSheet;
