export const generateColumnTemplate = (columns: string[]) =>
    'max-content max-content ' +
    columns.map(c => toCssUnit(`minmax(max-content, 'auto'})`)).join(' ');

const rowTemplate = 'calc(var(--grid-unit) * var(--row-height-multiplier))';
export const generateRowTemplate =(rows: string[]) => {

    return (
        rowTemplate +
        ' ' +
        rows
        .map(row => {
                return `${rowTemplate} auto`;
            })
            .join(' ')
    );
};

const toCssUnit = (value: number | string) => {
  if (typeof value === 'number') {
      return value + 'px';
  }

  return value;
};
