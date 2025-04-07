import React, { ButtonHTMLAttributes, CSSProperties, FC, forwardRef, useLayoutEffect } from "react";
import { PureComponent, useRef, useState } from "react";
import { Cropper, CropperImage, CropperPreview, CropperPreviewRef, CropperRef, CropperState, CropperTransitions, Size, getBackgroundStyle, getPreviewStyle, mergeRefs } from "react-advanced-cropper";
import "react-advanced-cropper/dist/style.css";
import "./ImageEditor.scss";
import { IconBrightnessUp, IconCircleCheckFilled, IconColorFilter, IconContrast, IconCrop, IconDropletHalf2Filled, IconRestore } from "@tabler/icons-react";
import { ColorScheme, useMantineColorScheme } from "@mantine/core";

interface ImageEditorProps {
    src:string,
    onOk:(imageBlob:Blob) => void
  }

export const ImageEditor = ({src, onOk}:ImageEditorProps) => {
    const cropperRef = useRef<CropperRef>(null);
    const previewRef = useRef<CropperPreviewRef>(null);
    const { colorScheme } = useMantineColorScheme();
  
    //const [src, setSrc] = useState(require("./photo.jpeg"));
  
    const [mode, setMode] = useState("crop");
  
    const [adjustments, setAdjustments] = useState({
      brightness: 0,
      hue: 0,
      saturation: 0,
      contrast: 0
    });
  
    const onChangeValue = (value: number) => {
      if (mode in adjustments) {
        setAdjustments((previousValue) => ({
          ...previousValue,
          [mode]: value
        }));
      }
    };
  
    const onReset = () => {
      setMode("crop");
      setAdjustments({
        brightness: 0,
        hue: 0,
        saturation: 0,
        contrast: 0
      });
    };
  
    // const onUpload = (blob: string) => {
    //   onReset();
    //   setMode("crop");
    //   setSrc(blob);
    // };
  
    const onNavigationOk = () => {
      if (cropperRef.current) {
        // const newTab = window.open();
        // if (newTab) {
        //   newTab.document.body.innerHTML = `<img src="${cropperRef.current
        //     .getCanvas()
        //     ?.toDataURL()}"/>`;
        // }

        //TODO: loading
        cropperRef.current.getCanvas()?.toBlob(imageBlob => {
            if (imageBlob) {
                onOk(imageBlob);
            }
        });
        
        //.toDataURL();

      }
    };
  
    const onUpdate = () => {
      previewRef.current?.refresh();
    };
  
    const changed = Object.values(adjustments).some((el) => Math.floor(el * 100));
  
    const cropperEnabled = mode === "crop";
  
    return (
      <div className={"image-editor"}>
        <div className="image-editor__cropper">
          <Cropper
            src={src}
            ref={cropperRef}
            stencilProps={{
              movable: cropperEnabled,
              resizable: cropperEnabled,
              lines: cropperEnabled,
              handlers: cropperEnabled,
              overlayClassName: cn(
                "image-editor__cropper-overlay",
                !cropperEnabled && "image-editor__cropper-overlay--faded"
              )
            }}
            backgroundWrapperProps={{
              scaleImage: cropperEnabled,
              moveImage: cropperEnabled
            }}
            backgroundComponent={AdjustableCropperBackground}
            backgroundProps={adjustments}
            onUpdate={onUpdate}
          />
          {mode !== "crop" && (
            <Slider
              className="image-editor__slider"
              value={(adjustments as Record<string,number>)[mode]}
              onChange={onChangeValue}
            />
          )}
          <CropperPreview
            className={"image-editor__preview"}
            ref={previewRef}
            cropper={cropperRef}
            backgroundComponent={AdjustablePreviewBackground}
            backgroundProps={adjustments}
          />
          <Button
            className={cn(
              "image-editor__reset-button",
              !changed && "image-editor__reset-button--hidden"
            )}
            onClick={onReset}
          >
            <IconRestore />
          </Button>
        </div>
        <Navigation
          mode={mode}
          onChange={setMode}
          //onUpload={onUpload}
          onOk={onNavigationOk}
        />
      </div>
    );
  };

  interface SliderProps {
    className?: string;
    onChange?: (value: number) => void;
    value?: number;
    showValue?: boolean;
  }
  
  export function Slider(props:SliderProps) {
    const { colorScheme } = useMantineColorScheme();
    return <SliderInternal colorScheme={colorScheme} {...props} />;
  }

  class SliderInternal extends PureComponent<SliderProps & { colorScheme:ColorScheme }> {
    line = React.createRef<HTMLDivElement>();
  
    state = {
      focus: false,
      width: 0
    };
  
    componentDidMount() {
      window.addEventListener("resize", this.recalculateWidth);
      window.addEventListener("orientationchange", this.recalculateWidth);
  
      window.addEventListener("mouseup", this.onStop, { passive: false });
      window.addEventListener("mousemove", this.onDrag, { passive: false });
      window.addEventListener("touchmove", this.onDrag, { passive: false });
      window.addEventListener("touchend", this.onStop, { passive: false });
  
      const line = this.line.current;
      if (line) {
        line.addEventListener("mousedown", this.onStart);
        line.addEventListener("touchstart", this.onStart);
      }
  
      this.recalculateWidth();
    }
    componentWillUnmount() {
      window.removeEventListener("mouseup", this.onStop);
      window.removeEventListener("mousemove", this.onDrag);
      window.removeEventListener("touchmove", this.onDrag);
      window.removeEventListener("touchend", this.onStop);
  
      window.removeEventListener("resize", this.recalculateWidth);
      window.removeEventListener("orientationchange", this.recalculateWidth);
  
      const line = this.line.current;
      if (line) {
        line.removeEventListener("mousedown", this.onStart);
        line.removeEventListener("touchstart", this.onStart);
      }
    }
    onDrag = (e: MouseEvent | TouchEvent) => {
      const { onChange } = this.props;
      if (this.state.focus) {
        const position = "touches" in e ? e.touches[0].clientX : e.clientX;
        const line = this.line.current;
  
        if (line) {
          const { left, width } = line.getBoundingClientRect();
  
          if (onChange) {
            onChange(
              Math.max(
                -1,
                Math.min(1, (2 * (position - left - width / 2)) / width)
              )
            );
          }
        }
        if (e.preventDefault) {
          e.preventDefault();
        }
      }
    };
    onStop = () => {
      this.setState({
        focus: false
      });
    };
    onStart = (e: MouseEvent | TouchEvent) => {
      this.setState({
        focus: true
      });
      this.onDrag(e);
    };
    recalculateWidth = () => {
      const line = this.line.current;
      if (line) {
        this.setState({
          width: line.clientWidth
        });
      }
    };
    render() {
      const { value = 0, className } = this.props;
  
      const handleInsideDot = this.state.width
        ? Math.abs(value) <= 16 / this.state.width
        : true;
  
      const fillWidth = `${Math.abs(value) * 50}%`;
  
      const fillLeft = `${50 * (1 - Math.abs(Math.min(0, value)))}%`;
  
      const formattedValue = `${value > 0 ? "+" : ""}${Math.round(100 * value)}`;
  
      return (
        <div className={cn("image-editor-slider", className!)} ref={this.line}>
          <div className="image-editor-slider__line">
            <div
              className="image-editor-slider__fill"
              style={{
                width: fillWidth,
                left: fillLeft
              }}
            />
            <div className="image-editor-slider__dot" />
            <div
              className={cn(
                "image-editor-slider__value",
                handleInsideDot && "image-editor-slider__value--hidden"
              )}
              style={{
                left: `${Math.abs(value * 50 + 50)}%`
              }}
            >
              {formattedValue}
            </div>
            <div
              className={cn(
                "image-editor-slider__handler",
                this.state.focus && "image-editor-slider__handler--focus",
                handleInsideDot && "image-editor-slider__handler--hidden"
              )}
              style={{
                left: `${value * 50 + 50}%`
              }}
            />
          </div>
        </div>
      );
    }
  }
  
  interface NavigationProps {
    className?: string;
    mode?: string;
    onChange?: (mode: string) => void;
    onOk?: () => void;
    //onUpload?: (blob: string) => void;
  }
  
  export const Navigation: FC<NavigationProps> = ({
    className,
    onChange,
    //onUpload,
    onOk,
    mode
  }) => {
    const { colorScheme } = useMantineColorScheme();
    const setMode = (mode: string) => () => {
      onChange?.(mode);
    };
  
    //const inputRef = useRef<HTMLInputElement>(null);
  
    // const onUploadButtonClick = () => {
    //   inputRef.current?.click();
    // };
  
    // const onLoadImage = (event: ChangeEvent<HTMLInputElement>) => {
    //   // Reference to the DOM input element
    //   const { files } = event.target;
  
    //   // Ensure that you have a file before attempting to read it
    //   if (files && files[0]) {
    //     if (onUpload) {
    //       onUpload(URL.createObjectURL(files[0]));
    //     }
    //   }
    //   // Clear the event target value to give the possibility to upload the same image:
    //   event.target.value = "";
    // };
  
    return (
      <div className={cn("image-editor-navigation", className)}>
        {/* <Button
          className={"image-editor-navigation__button"}
          onClick={onUploadButtonClick}
        >
          <UploadIcon />
          <input
            ref={inputRef}
            type="file"
            accept="image/*"
            onChange={onLoadImage}
            className="image-editor-navigation__upload-input"
          />
        </Button> */}
        <div className="image-editor-navigation__buttons">
          <Button
            className={"image-editor-navigation__button"}
            active={mode === "crop"}
            onClick={setMode("crop")}
          >
            <IconCrop />
          </Button>
          <Button
            className={"image-editor-navigation__button"}
            active={mode === "saturation"}
            onClick={setMode("saturation")}
          >
            <IconDropletHalf2Filled />
          </Button>
          <Button
            className={"image-editor-navigation__button"}
            active={mode === "brightness"}
            onClick={setMode("brightness")}
          >
            <IconBrightnessUp />
          </Button>
          <Button
            className={"image-editor-navigation__button"}
            active={mode === "contrast"}
            onClick={setMode("contrast")}
          >
            <IconContrast />
          </Button>
          <Button
            className={"image-editor-navigation__button"}
            active={mode === "hue"}
            onClick={setMode("hue")}
          >
            <IconColorFilter />
          </Button>
        </div>
        <Button
          className={"image-editor-navigation__button__ok"}
          onClick={onOk}
        >
          <IconCircleCheckFilled />
        </Button>
      </div>
    );
  };
  
  interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
	active?: boolean;
}

export const Button: FC<ButtonProps> = ({ className, active, children, ...props }) => {
    const { colorScheme } = useMantineColorScheme();
	return (
		<button className={cn('image-editor-button', active && 'image-editor-button--active', className)} {...props}>
			{children}
		</button>
	);
};

interface DesiredCropperRef {
	getState: () => CropperState;
	getTransitions: () => CropperTransitions;
	getImage: () => CropperImage;
}

interface AdjustablePreviewBackgroundProps {
	className?: string;
	cropper: DesiredCropperRef;
	crossOrigin?: 'anonymous' | 'use-credentials' | boolean;
	brightness?: number;
	saturation?: number;
	hue?: number;
	contrast?: number;
	size?: Size | null;
}

export const AdjustablePreviewBackground = ({
	className,
	cropper,
	crossOrigin,
	brightness = 0,
	saturation = 0,
	hue = 0,
	contrast = 0,
	size,
}: AdjustablePreviewBackgroundProps) => {
	const state = cropper.getState();
	const transitions = cropper.getTransitions();
	const image = cropper.getImage();

	const style = image && state && size ? getPreviewStyle(image, state, size, transitions) : {};

	return (
		<AdjustableImage
			src={image?.src}
			crossOrigin={crossOrigin}
			brightness={brightness}
			saturation={saturation}
			hue={hue}
			contrast={contrast}
			className={className}
			style={style}
		/>
	);
};


interface AdjustableImageProps {
	src?: string;
	className?: string;
	crossOrigin?: 'anonymous' | 'use-credentials' | boolean;
	brightness?: number;
	saturation?: number;
	hue?: number;
	contrast?: number;
	style?: CSSProperties;
}

export const AdjustableImage = forwardRef<HTMLCanvasElement, AdjustableImageProps>(
	({ src, className, crossOrigin, brightness = 0, saturation = 0, hue = 0, contrast = 0, style }: AdjustableImageProps, ref) => {
		const imageRef = useRef<HTMLImageElement>(null);
		const canvasRef = useRef<HTMLCanvasElement>(null);

		const drawImage = () => {
			const image = imageRef.current;
			const canvas = canvasRef.current;
			if (canvas && image && image.complete) {
				const ctx = canvas.getContext('2d');
				canvas.width = image.naturalWidth;
				canvas.height = image.naturalHeight;

				if (ctx) {
					ctx.filter = [
						`brightness(${100 + brightness * 100}%)`,
						`contrast(${100 + contrast * 100}%)`,
						`saturate(${100 + saturation * 100}%)`,
						`hue-rotate(${hue * 360}deg)`,
					].join(' ');

					ctx.drawImage(image, 0, 0, image.naturalWidth, image.naturalHeight);
				}
			}
		};

		useLayoutEffect(() => {
			drawImage();
		}, [src, brightness, saturation, hue, contrast]);

		return (
			<>
				<canvas
					key={`${src}-canvas`}
					ref={mergeRefs([ref, canvasRef])}
					className={cn('adjustable-image-element', className)}
					style={style}
				/>
				{src ? (
					<img
						key={`${src}-img`}
						ref={imageRef}
						className={'adjustable-image-source'}
						src={src}
						crossOrigin={crossOrigin === true ? 'anonymous' : crossOrigin || undefined}
						onLoad={drawImage}
					/>
				) : null}
			</>
		);
	},
);

AdjustableImage.displayName = 'AdjustableImage';

interface DesiredCropperRef {
    getState: () => CropperState;
    getTransitions: () => CropperTransitions;
    getImage: () => CropperImage;
  }
  
  interface Props {
    className?: string;
    cropper: DesiredCropperRef;
    crossOrigin?: "anonymous" | "use-credentials" | boolean;
    brightness?: number;
    saturation?: number;
    hue?: number;
    contrast?: number;
  }
  
  export const AdjustableCropperBackground = forwardRef<HTMLCanvasElement, Props>(
    (
      {
        className,
        cropper,
        crossOrigin,
        brightness = 0,
        saturation = 0,
        hue = 0,
        contrast = 0
      }: Props,
      ref
    ) => {
      const state = cropper.getState();
      const transitions = cropper.getTransitions();
      const image = cropper.getImage();
  
      const style =
        image && state ? getBackgroundStyle(image, state, transitions) : {};
  
      return (
        <AdjustableImage
          src={image?.src}
          crossOrigin={crossOrigin}
          brightness={brightness}
          saturation={saturation}
          hue={hue}
          contrast={contrast}
          ref={ref}
          className={className}
          style={style}
        />
      );
    }
  );
  
  AdjustableCropperBackground.displayName = 'AdjustableCropperBackground';


  const hasOwn = {}.hasOwnProperty;

  function classNames(...args: (string | number | Record<string,string> | boolean | undefined)[]): string {
      let classes = '';
  
      for (let i = 0; i < args.length; i++) {
          const arg = args[i];
          if (arg) {
              classes = appendClass(classes, parseValue(arg));
          }
      }
  
      return classes;
  }
  
  const cn = classNames;
  
  function parseValue(arg: string | number | Record<string,string> | boolean | undefined): string {
      if (typeof arg === 'string' || typeof arg === 'number') {
          return arg.toString();
      }
  
      if (typeof arg !== 'object' || arg === null) {
          return '';
      }
  
      if (Array.isArray(arg)) {
          return classNames(...arg);
      }
  
      if (arg.toString !== Object.prototype.toString && !arg.toString.toString().includes('[native code]')) {
          return arg.toString();
      }
  
      let classes = '';
  
      for (const key in arg) {
          if (Object.prototype.hasOwnProperty.call(arg, key) && arg[key]) {
              classes = appendClass(classes, key);
          }
      }
  
      return classes;
  }
  
  function appendClass(value: string, newClass: string): string {
      if (!newClass) {
          return value;
      }
  
      return value ? `${value} ${newClass}` : newClass;
  }
  