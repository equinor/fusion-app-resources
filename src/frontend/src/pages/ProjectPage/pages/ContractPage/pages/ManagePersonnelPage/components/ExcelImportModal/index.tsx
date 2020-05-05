import * as React from 'react';
import * as styles from './styles.less';
import { Spinner, OverlayPortal, Scrim, Button } from '@equinor/fusion-components';

type ExcelImportModalProps = {
    setSelectedFile: React.Dispatch<React.SetStateAction<File | null>>;
    isProccessing: boolean;
    isOpen: boolean;
    setIsOpen: React.Dispatch<React.SetStateAction<boolean>>;
};

const ExcelImportModal: React.FC<ExcelImportModalProps> = ({
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

    const startProssessingSelectedFile = React.useCallback(() => {
        setSelectedFile(selectedFileForUpload);
    }, [selectedFileForUpload]);

    const stopPropagationAndDefault = React.useCallback((e: React.DragEvent<HTMLDivElement>) => {
        e.stopPropagation();
        e.preventDefault();
    }, []);

    const closeModal = React.useCallback(() => {
        !isProccessing && setIsOpen(false);
    }, [isProccessing]);

    return (
        <OverlayPortal show={isOpen}>
            <Scrim onClick={closeModal} show={isOpen}>
                <div onClick={(e) => e.stopPropagation()} className={styles.excelImportModal}>
                    {isProccessing && (
                        <div className={styles.prossessing}>
                            <Spinner centered title="Prossessing Excel file" />
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
                                    <p>Drag and drop a excel file here</p>
                                    <p>or </p>
                                    <div className={styles.fileInput}>
                                        <div className={styles.inputButton}>
                                            <Button onClick={fileInputClick}>Select a file</Button>
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
                            <div className={styles.prossessButton}>
                                <Button
                                    disabled={!selectedFileForUpload}
                                    onClick={startProssessingSelectedFile}
                                >
                                    <p className={styles.prossessButtonText}>
                                        Process selected excel file
                                    </p>
                                </Button>
                            </div>
                        </div>
                    )}
                </div>
            </Scrim>
        </OverlayPortal>
    );
};

export default ExcelImportModal;
